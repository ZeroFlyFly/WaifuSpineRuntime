using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spine.Unity
{
    [ExecuteAlways]
    public class VendorsGenerator : MonoBehaviour
    {
        public class DecodeGeometry
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector2> uv = new List<Vector2>();

            public List<int> index =  new List<int>();
        }

        public class DecodeSkinning
        {
            public List<string> nameList = new List<string>();

            public List<Matrix4x4> matrixList = new List<Matrix4x4>();
        }

        [SerializeField]
        [Header("Geo层级文件")]
        TextAsset geoJson;

        private string jsonContent;

        private static Dictionary<string, DecodeGeometry> decodeGeometryDict = new Dictionary<string, DecodeGeometry>();

        private static Dictionary<string, DecodeSkinning> decodeSkinningDict = new Dictionary<string, DecodeSkinning>();

        private static Dictionary<string, Transform> nameToTransform = new Dictionary<string, Transform>();

        [SerializeField]
        [Header("模型默认旋转")]
        private Vector3 verticesRotation = Vector3.zero;

        [SerializeField]
        [Header("模型默认缩放")]
        private Vector3 verticesScale = Vector3.one;

        [SerializeField]
        [Header("翻转法线")]
        private bool flipNormal = false;

        [SerializeField]
        [Header("全局缩放(参考Spine文件)")]
        private float GlobalScale = 0.01f;

        [SerializeField]
        [Header("Anim文件")]
        List<TextAsset> animJsonList = new List<TextAsset>();

        private static string eventName = "";

        // Start is called before the first frame update
        public void SpawnHierachy()
        {
            if (geoJson == null)
            {
                Debug.LogError("Geometry Json File is Empty!!!");
                return;
            }

            int underPos = geoJson.name.LastIndexOf('_');

            eventName = geoJson.name.Substring(0, underPos);

            decodeGeometryDict.Clear();
            decodeSkinningDict.Clear();

            jsonContent = geoJson.text;

            SharpJson.JsonDecoder parser = new SharpJson.JsonDecoder();
            parser.parseNumbersAsFloat = true;

            object decodeJson = parser.Decode(jsonContent);

            Dictionary<string, object> root = decodeJson as Dictionary<string, object>;

            nameToTransform.Clear();

            if (root.ContainsKey("geometries"))
            {
                Dictionary<string, object> geometries = (Dictionary<string, object>)root["geometries"];

                foreach(var pair in geometries)
                {
                    string key = pair.Key;

                    DecodeGeometry newGeo = new DecodeGeometry();

                    Dictionary<string, object> geoParameter = (Dictionary<string, object>)pair.Value;

                    if (geoParameter.ContainsKey("attributes"))
                    {
                        Dictionary<string, object> geoAttributes = (Dictionary<string, object>)geoParameter["attributes"];

                        if (geoAttributes.ContainsKey("position"))
                        {
                            Dictionary<string, object> positionDict = (Dictionary<string, object>)geoAttributes["position"];

                            List<object> positionList = (List<object>)positionDict["array"];

                            for(int i = 0; i < positionList.Count; i += 3)
                            {
                                float x = (float)positionList[i];
                                float y = (float)positionList[i + 1];
                                float z = (float)positionList[i + 2];

                                Vector3 newVertices = new Vector3(x, y, -z);

                                newVertices *= GlobalScale;

                                newVertices.Scale(verticesScale);

                                newVertices = Quaternion.Euler(verticesRotation) * newVertices;

                                newGeo.vertices.Add(newVertices);
                            }
                        }

                        if (geoAttributes.ContainsKey("uv"))
                        {
                            Dictionary<string, object> uvDict = (Dictionary<string, object>)geoAttributes["uv"];

                            List<object> uvList = (List<object>)uvDict["array"];

                            for (int i = 0; i < uvList.Count; i += 2)
                            {
                                float x = (float)uvList[i];
                                float y = (float)uvList[i + 1];

                                Vector2 newUV = new Vector2(x, y);

                                newGeo.uv.Add(newUV);
                            }
                        }
                    }

                    if (geoParameter.ContainsKey("index"))
                    {
                        List<object> indexList = (List<object>)geoParameter["index"];

                        for (int i = 0; i < indexList.Count; i+= 3)
                        {
                            string c = "" + indexList[i];
                            int x = int.Parse(c);

                            string d = "" + indexList[i + 1];
                            int y = int.Parse(d);

                            string e = "" + indexList[i + 2];

                            int z = int.Parse(e);

                            if (flipNormal)
                            {
                                newGeo.index.Add(y);
                                newGeo.index.Add(x);
                                newGeo.index.Add(z);
                            }
                            else
                            {
                                newGeo.index.Add(x);
                                newGeo.index.Add(y);
                                newGeo.index.Add(z);
                            }
                            
                        }
                    }

                    decodeGeometryDict.Add(key, newGeo);
                }
            }

            if (root.ContainsKey("skinning"))
            {
                Dictionary<string, object> skinning = (Dictionary<string, object>)root["skinning"];

                foreach (var pair in skinning)
                {
                    string key = pair.Key;

                    DecodeSkinning newSkinning = new DecodeSkinning();

                    Dictionary<string, object> skinDict = (Dictionary<string, object>)pair.Value;

                    if (skinDict.ContainsKey("bones"))
                    {
                        List<object> skinList = (List<object>)skinDict["bones"];

                        for (int i = 0; i < skinList.Count; i ++)
                        {
                            Dictionary<string, object> nameDict = (Dictionary<string, object>)skinList[i];

                            if (nameDict.ContainsKey("name"))
                            {
                                string name = (string)nameDict["name"];

                                newSkinning.nameList.Add(name);
                            }
                        }
                    }

                    if (skinDict.ContainsKey("matrix"))
                    {
                        List<object> matrixLines = (List<object>)skinDict["matrix"];

                        for (int i = 0; i < matrixLines.Count; i++)
                        {
                            List<object> curMatrix = (List<object>)matrixLines[i];

                            List<Vector4> vectorList = new List<Vector4>();

                            for(int j = 0; j < curMatrix.Count; j += 4)
                            {
                                float x = (float)curMatrix[j];
                                float y = (float)curMatrix[j + 1];
                                float z = (float)curMatrix[j + 2];
                                float w = (float)curMatrix[j + 3];

                                vectorList.Add(new Vector4(x, y, z, w));
                            }

                            Matrix4x4 newMatrix = new Matrix4x4(vectorList[0], vectorList[1], vectorList[2], vectorList[3]);

                            newSkinning.matrixList.Add(newMatrix);
                        }
                    }

                    decodeSkinningDict.Add(key, newSkinning);
                }
            }

            List<Transform> toDestroy = new List<Transform>();

            for(int i = 0; i < transform.childCount; i++)
            {
                toDestroy.Add(transform.GetChild(i));
            }

            for(int i = 0; i < toDestroy.Count; i++)
            {
                DestroyImmediate(toDestroy[i].gameObject);
            }

            toDestroy.Clear();

            if (root.ContainsKey("sceneList"))
            {
                List<object> sceneList = (List<object>)root["sceneList"];

                foreach(var o in sceneList)
                {
                    Dictionary<string, object> sceneDict = (Dictionary<string, object>)o;

                    string sceneName = "";
                    if (sceneDict.ContainsKey("id"))
                    {
                        sceneName = (string)sceneDict["id"];
                    }

                    GameObject sceneRoot = new GameObject(sceneName);

                    sceneRoot.transform.parent = transform;

                    sceneRoot.transform.localPosition = Vector3.zero;

                    sceneRoot.transform.localRotation = Quaternion.identity;

                    if(!nameToTransform.ContainsKey(sceneName))
                        nameToTransform.Add(sceneName, sceneRoot.transform);

                    if (sceneDict.ContainsKey("children"))
                    {
                        List<object> childrenList = (List<object>)sceneDict["children"];
#if UNITY_EDITOR
                        IterateChildren(childrenList, sceneRoot.transform, GlobalScale);
#endif
                    }
                }
            }

#if UNITY_EDITOR        
            foreach (var animText in animJsonList)
            {
                string animContent = animText.text;

                Dictionary<string, object> animRoot = parser.Decode(animContent) as Dictionary<string, object>;

                foreach(var pair in animRoot)
                {
                    try
                    {
                        Dictionary<string, object> animInfo = (Dictionary<string, object>)pair.Value;

                        IterateAnimClip(animInfo, "", -1, transform.root, null);
                    }
                    catch(System.Exception e)
                    {
                        Debug.LogError("Falied to Cast Animation, Check File :" + animText.name + " Content Please.");

                        break;
                    }
                }
                
            }
#endif
        }
#if UNITY_EDITOR
        public static void IterateChildren(List<object> childrenList, Transform parent,float globalScale)
        {
            foreach(var obj in childrenList)
            {
                Dictionary<string, object> child = (Dictionary<string, object>)obj;

                string childName = "";

                if (child.ContainsKey("name"))
                {
                    childName = (string)child["name"];
                }

                int childType = int.Parse("" + child["type"]);

                int renderOrder = 0;

                if (child.ContainsKey("renderOrder"))
                {
                    float fRender = 0f;
                    bool tryParse = float.TryParse("" + child["renderOrder"], out fRender);
                    if (tryParse)
                    {
                        renderOrder = (int)fRender;
                    }
                }

                GameObject childGO = new GameObject(childName);

                childGO.transform.parent = parent;

                childGO.transform.localPosition = Vector3.zero;
                childGO.transform.localRotation = Quaternion.identity;

                if (!nameToTransform.ContainsKey(childName))
                    nameToTransform.Add(childName, childGO.transform);

                if (child.ContainsKey("position"))
                {
                    List<object> positionList = (List<object>)child["position"];

                    for (int i = 0; i < positionList.Count; i += 3)
                    {
                        float x = (float)positionList[i];
                        float y = (float)positionList[i + 1];
                        float z = (float)positionList[i + 2];

                        Vector3 newPos = new Vector3(x, y, -z);

                        newPos *= globalScale;

                        childGO.transform.localPosition = newPos;
                    }
                }

                if (child.ContainsKey("rotation"))
                {
                    List<object> rotationList = (List<object>)child["rotation"];

                    for (int i = 0; i < rotationList.Count; i += 3)
                    {
                        float x = (float)rotationList[i];
                        float y = (float)rotationList[i + 1];
                        float z = (float)rotationList[i + 2];

                        Vector3 newRot = new Vector3(x, y, z);

                        newRot *= Mathf.Rad2Deg;

                        childGO.transform.localRotation = Quaternion.Euler(newRot.x,newRot.y,newRot.z);
                    }
                }

                if (child.ContainsKey("scale"))
                {
                    List<object> scaleList = (List<object>)child["scale"];

                    for (int i = 0; i < scaleList.Count; i += 3)
                    {
                        float x = (float)scaleList[i];
                        float y = (float)scaleList[i + 1];
                        float z = (float)scaleList[i + 2];

                        Vector3 newScale = new Vector3(x, y, z);

                        if(childGO.transform != null)
                        {
                            newScale = Vector3.Scale(childGO.transform.parent.localScale, newScale);
                        }

                        childGO.transform.localScale = newScale;
                    }
                }

                MeshRenderer generateMesh = null;

                if (child.ContainsKey("geometry"))
                {
                    Dictionary<string, object> geoDict = (Dictionary<string, object>)child["geometry"];

                    int iType = int.Parse("" + geoDict["type"]);

                    switch (iType)
                    {
                        case 1:
                            string id = (string)geoDict["id"];
                            if (decodeGeometryDict.ContainsKey(id))
                            {
                                DecodeGeometry geo = decodeGeometryDict[id];
                                generateMesh = CreateGeometry(childName, eventName, geo, childGO);
                            }
                            break;
                        case 2:
                            Dictionary<string, object> config = (Dictionary<string, object>)geoDict["config"];

                            float width = 0;
                            float height = 0;

                            if (config.ContainsKey("width"))
                            {
                                width = float.Parse("" + config["width"]);
                            }

                            if (config.ContainsKey("height"))
                            {
                                height = float.Parse("" + config["height"]);
                            }

                            if(width > 0 && height > 0)
                            {
                                generateMesh = CreateQuadMesh(childName, eventName,width, height, childGO,globalScale);
                            }
                            break;

                    }
                }

                if (child.ContainsKey("material"))
                {
                    List<object> materialConfigList = (List<object>)child["material"];

                    foreach (var currentMaterial in materialConfigList)
                    {
                        Dictionary<string, object> materialConfig = (Dictionary<string, object>)currentMaterial;

                        string materialStrID = "";

                        if (materialConfig.ContainsKey("id"))
                        {
                            materialStrID = "" + materialConfig["id"];
                        }

                        if (materialConfig.ContainsKey("uniforms"))
                        {
                            string valueImgFolder = Application.dataPath + "/" + eventName + "/" + "otherResources" + "/";

                            string valueImgPath = valueImgFolder + childName + ".png";

                            string materialPath = valueImgFolder + "Material/";

                            string relateMaterialPath = "Assets" + "/" + eventName + "/" + "otherResources" + "/" + "Material/";

                            if (!Directory.Exists(materialPath))
                            {
                                Directory.CreateDirectory(materialPath);
                            }

                            string relativeMaterialPath = relateMaterialPath + "M_" + childName + ".mat";

                            Material relateMaterial = null;

                            bool isBezierMaterial = false;

                            if (!File.Exists(relativeMaterialPath))
                            {
                                if (materialStrID.Equals("BEZIER_PARTICLE"))
                                {
                                    isBezierMaterial = true;

                                    Material defautMaterial = AssetDatabase.LoadAssetAtPath("Assets/Spine/Editor/spine-unity/Editor/Shaders/BezierParticleMaterial.mat", typeof(Material)) as Material;
                                    relateMaterial = new Material(defautMaterial);
                                }
                                else
                                {
                                    Material defautMaterial = AssetDatabase.LoadAssetAtPath("Assets/Spine/Editor/spine-unity/Editor/Shaders/BuildIn_Material_Template.mat", typeof(Material)) as Material;
                                    relateMaterial = new Material(defautMaterial);
                                }

                                Vector4 _SpeedSize = new Vector4(0.5f * globalScale * 10, 1 * globalScale * 10, 20 * globalScale, 30 * globalScale);
                                Vector4 _AlphaLifeCurveImgNum = new Vector4(0.5f, 1, 1, 1);
                                Vector4 _PosFractStartTimeSeedTailLen = new Vector4(0, 0, 0, 1);
                                //_PosFractStartTimeSeedTailLen.x = Random.Range(-1.0f, 1.0f);
                                _PosFractStartTimeSeedTailLen.y = Time.timeSinceLevelLoad;
                                Vector4 _ScaleRotRND = new Vector4(1, 1, 0, 0);

                                List<object> uniformList = (List<object>)materialConfig["uniforms"];

                                foreach (var uniObj in uniformList)
                                {
                                    Dictionary<string, object> uniDict = (Dictionary<string, object>)uniObj;

                                    if (uniDict.ContainsKey("type") && uniDict.ContainsKey("key") && uniDict.ContainsKey("value"))
                                    {
                                        string type = (string)uniDict["type"];

                                        string key = (string)uniDict["key"];

                                        if (type.Equals("t"))
                                        {
                                            if (key.Equals("diffuse"))
                                            {
                                                string value = (string)uniDict["value"];

                                                string relativePathToAsset = "Assets" + "/" + eventName + "/" + "otherResources" + "/" + value + ".png";

                                                Texture2D tex2D = AssetDatabase.LoadAssetAtPath(relativePathToAsset, typeof(Texture2D)) as Texture2D;

                                                relateMaterial.SetTexture("_MainTex", tex2D);

                                                relateMaterial.SetTexture("_EmissionMap", tex2D);
                                            }
                                        }

                                        if (materialStrID.Equals("BEZIER_PARTICLE"))
                                        {
                                            if (key.Equals("size"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                _SpeedSize.z = value.x;
                                                _SpeedSize.w = value.y;
                                            }

                                            if (key.Equals("speed"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                //value *= globalScale * 10;

                                                _SpeedSize.x = value.x;
                                                _SpeedSize.y = value.y;
                                            }

                                            if (key.Equals("alpha"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                _AlphaLifeCurveImgNum.x = value.x;
                                                _AlphaLifeCurveImgNum.y = value.y;
                                            }

                                            if (key.Equals("startMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_StartMin", value);
                                            }

                                            if (key.Equals("startMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_StartMax", value);
                                            }

                                            if (key.Equals("ctrMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_CtrMin", value);
                                            }

                                            if (key.Equals("ctrMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_CtrMax", value);
                                            }

                                            if (key.Equals("endMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_EndMin", value);
                                            }

                                            if (key.Equals("endMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_EndMax", value);
                                            }

                                            if (key.Equals("freqMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_FreqMin", value);
                                            }

                                            if (key.Equals("freqMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_FreqMax", value);
                                            }

                                            if (key.Equals("ampMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_AmpMin", value);
                                            }

                                            if (key.Equals("ampMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_AmpMax", value);
                                            }

                                            if (key.Equals("delayMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_DelayMin", value);
                                            }

                                            if (key.Equals("delayMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_DelayMax", value);
                                            }

                                            if (key.Equals("scale"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                _ScaleRotRND.x = value.x;
                                                _ScaleRotRND.y = value.y;
                                            }

                                            if (key.Equals("rotRnd"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                _ScaleRotRND.z = value.x;
                                                _ScaleRotRND.w = value.y;
                                            }

                                            if (key.Equals("rotMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_RotMin", value);
                                            }

                                            if (key.Equals("rotMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_RotMax", value);
                                            }

                                            if (key.Equals("gravityMin"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_GravityMin", value);
                                            }

                                            if (key.Equals("gravityMax"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                value *= globalScale;

                                                relateMaterial.SetVector("_GravityMax", value);
                                            }

                                            if (key.Equals("blink"))
                                            {
                                                Vector4 value = (Vector4)GetSingleValueByType(uniDict, "value", type);

                                                relateMaterial.SetVector("_Blink", value);
                                            }

                                            if (key.Equals("imgNum"))
                                            {
                                                float value = (float)GetSingleValueByType(uniDict, "value", type);

                                                _AlphaLifeCurveImgNum.w = value;
                                            }
                                        }
                                    }
                                }

                                if (materialStrID.Equals("BEZIER_PARTICLE"))
                                {
                                    relateMaterial.SetVector("_SpeedSize", _SpeedSize);
                                    relateMaterial.SetVector("_AlphaLifeCurveImgNum", _AlphaLifeCurveImgNum);
                                    relateMaterial.SetVector("_PosFractStartTimeSeedTailLen", _PosFractStartTimeSeedTailLen);
                                    relateMaterial.SetVector("_ScaleRotRND", _ScaleRotRND);
                                }

                                if (materialConfig.ContainsKey("defines"))
                                {
                                    Dictionary<string, object> defineConfig = (Dictionary<string, object>)materialConfig["defines"];

                                    foreach (var pair in defineConfig)
                                    {
                                        string key = "_" + pair.Key;

                                        float value = 0;
                                        bool success = float.TryParse("" + pair.Value, out value);

                                        if (success)
                                        {
                                            relateMaterial.SetFloat(key, value);
                                        }
                                    }
                                }
                                AssetDatabase.CreateAsset(relateMaterial, relativeMaterialPath);
                                AssetDatabase.SaveAssets();
                            }

                            relateMaterial = AssetDatabase.LoadAssetAtPath(relativeMaterialPath, typeof(Material)) as Material;

                            if(isBezierMaterial)
                            {
                                if (child.ContainsKey("pluginId") && child.ContainsKey("userData"))
                                {
                                    string pluginID = "" + child["pluginId"];

                                    Dictionary<string, object> userDataDict = (Dictionary<string, object>)child["userData"];

                                    if (userDataDict.ContainsKey("pluginData"))
                                    {
                                        Dictionary<string, object> pluginDataDict = (Dictionary<string, object>)userDataDict["pluginData"];

                                        int count = 0;

                                        if (pluginDataDict.ContainsKey("count"))
                                        {
                                            count = int.Parse("" + pluginDataDict["count"]);
                                        }

                                        if (count > 0)
                                        {
                                            DrawSpineParticleInstance dspi = childGO.AddComponent<DrawSpineParticleInstance>();

                                            dspi.population = count;

                                            dspi.material = relateMaterial;

                                            if (generateMesh != null)
                                            {
                                                generateMesh.enabled = false;
                                            }
                                        }
                                    }
                                }
                            }

                            if(generateMesh != null)
                            {
                                generateMesh.material = relateMaterial;
                            }
                        }
                    }
                }

                if (child.ContainsKey("spine"))
                {
                    Dictionary<string, object> spineInfo = (Dictionary<string, object>)child["spine"];

                    string id = (string)spineInfo["id"];

                    string spinePath = "Assets/" + eventName + "/" + id + "/" + id + "_SkeletonData.asset";

                    SkeletonDataAsset sda = AssetDatabase.LoadAssetAtPath(spinePath, typeof(SkeletonDataAsset)) as SkeletonDataAsset;

                    if (sda != null)
                    {
                        SkeletonAnimation sa = childGO.AddComponent<SkeletonAnimation>();

                        MeshRenderer spineRenderer = sa.GetComponent<MeshRenderer>();

                        spineRenderer.sortingOrder = renderOrder;

                        sa.skeletonDataAsset = sda;

                        sa.loop = true;

                        string defaultAnimation = "animation";

                        //Debug.Log(id + " " + sda.GetAnimationStateData().SkeletonData.Animations.Count);

                        if (sda.GetAnimationStateData().SkeletonData.Animations.Count > 0)
                        {
                            Animation[] animationList = sda.GetAnimationStateData().SkeletonData.Animations.ToArray();
                            defaultAnimation = animationList[0].Name;
                        }

                        if (spineInfo.ContainsKey("defaultAnimation"))
                        {
                            defaultAnimation = (string)spineInfo["defaultAnimation"];
                        }

                        if (spineInfo.ContainsKey("skin"))
                        {
                            string initialSkin = (string)spineInfo["skin"];

                            sa.initialSkinName = initialSkin;
                        }

                        if (!string.IsNullOrEmpty(defaultAnimation))
                        {
                            sa.AnimationName = defaultAnimation;
                        }

                        if (spineInfo.ContainsKey("timeScale"))
                        {
                            float timeScale = float.Parse("" + spineInfo["timeScale"]);

                            sa.timeScale = timeScale;
                        }

                        SkeletonUtility su = childGO.AddComponent<SkeletonUtility>();

                        su.SpawnHierarchy(SkeletonUtilityBone.Mode.Follow, true, true, true);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Failed to Load Path : " + spinePath);
                    }
                }

                if (child.ContainsKey("visible"))
                {
                    MeshRenderer currentRenderer = childGO.GetComponent<MeshRenderer>();

                    if(currentRenderer != null && currentRenderer.enabled)
                        currentRenderer.enabled = (bool)child["visible"];
                }

                if (child.ContainsKey("children"))
                {
                    List<object> nextChildrenList = (List<object>)child["children"];

                    IterateChildren(nextChildrenList, childGO.transform,globalScale);
                }
            }
        }

        public static AnimationClip IterateAnimClip(Dictionary<string,object> currentInfo, string parentName, int parentType, Transform root, AnimationClip targetClip)
        {
            int type = -1;

            int start = -1;

            bool loop = false;

            string name = "";

            string valueType = "";

            if (currentInfo.ContainsKey("type"))
            {
                type = int.Parse("" + currentInfo["type"]);
            }

            if (currentInfo.ContainsKey("start"))
            {
                start = int.Parse("" + currentInfo["start"]);
            }

            if (currentInfo.ContainsKey("loop"))
            {
                loop = bool.Parse("" + currentInfo["loop"]);
            }

            if (currentInfo.ContainsKey("name"))
            {
                name = "" + currentInfo["name"];
            }

            if (currentInfo.ContainsKey("valueType"))
            {
                valueType = "" + currentInfo["valueType"];
            }

            if(parentType == 1 && type == 2)//  type != 1)  Only Consider Position And Rotation First
            {
                if(targetClip == null)
                {
                    targetClip = new AnimationClip();

                    targetClip.legacy = true;

                    targetClip.name = parentName;
                }

                if (currentInfo.ContainsKey("data"))
                {
                    List<object> dataList = (List<object>)currentInfo["data"];

                    string[] nameList = name.Split('.');

                    string targetTransformName = nameList[0];

                    string featureName = nameList.Length > 1 ? nameList[nameList.Length - 1] : "";

                    List<List<Keyframe>> keyFrameList = new List<List<Keyframe>>();

                    if (featureName.Equals("position") || featureName.Equals("quaternion"))
                    {
                        int paramCount = 0;

                        if (valueType.Equals("v3"))
                        {
                            paramCount = 3;
                        }
                        else if (valueType.Equals("q") || valueType.Equals("v4"))
                        {
                            paramCount = 4;
                        }
                        for (int i = 0; i < paramCount; i++)
                        {
                            keyFrameList.Add(new List<Keyframe>());
                        }
                    }

                    foreach (var dataObj in dataList)
                    {
                        Dictionary<string, object> dataInfo = (Dictionary<string, object>)dataObj;

                        IterateDataClip(dataInfo, targetTransformName, featureName, valueType, start, root, ref keyFrameList);
                    }

                    if (keyFrameList.Count > 0 && keyFrameList[0].Count > 0)
                    {
                        string fullTransformPath = GetGameObjectPath(nameToTransform[targetTransformName].gameObject);

                        fullTransformPath = fullTransformPath.Substring(fullTransformPath.IndexOf('/') + 1);

                        if (featureName.Equals("position"))
                        {
                            AnimationCurve postionXcurve = new AnimationCurve(keyFrameList[0].ToArray());
                            AnimationCurve postionYcurve = new AnimationCurve(keyFrameList[1].ToArray());
                            AnimationCurve postionZcurve = new AnimationCurve(keyFrameList[2].ToArray());

                            SetCurveLinear(postionXcurve);
                            SetCurveLinear(postionYcurve);
                            SetCurveLinear(postionZcurve);

                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localPosition.x", postionXcurve);
                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localPosition.y", postionYcurve);
                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localPosition.z", postionZcurve);
                        }

                        if (featureName.Equals("quaternion"))
                        {
                            AnimationCurve rotationXcurve = new AnimationCurve(keyFrameList[0].ToArray());
                            AnimationCurve rotationYcurve = new AnimationCurve(keyFrameList[1].ToArray());
                            AnimationCurve rotationZcurve = new AnimationCurve(keyFrameList[2].ToArray());
                            AnimationCurve rotationWcurve = new AnimationCurve(keyFrameList[3].ToArray());

                            SetCurveLinear(rotationXcurve);
                            SetCurveLinear(rotationYcurve);
                            SetCurveLinear(rotationZcurve);
                            SetCurveLinear(rotationWcurve);

                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localRotation.x", rotationXcurve);
                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localRotation.y", rotationYcurve);
                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localRotation.z", rotationZcurve);
                            targetClip.SetCurve(fullTransformPath, typeof(Transform), "localRotation.w", rotationWcurve);
                        }
                    }
                }
            }
            else if (currentInfo.ContainsKey("clips"))
            {
                List<object> clipList = (List<object>)currentInfo["clips"];

                if (parentType == 1 && type == 1)
                {
                    targetClip = null;
                }

                foreach (var clipObj in clipList)
                {
                    Dictionary<string, object> clipInfo = (Dictionary<string, object>)clipObj;

                    targetClip = IterateAnimClip(clipInfo, name, type, root, targetClip);
                }

                if(type == 1 && targetClip != null)
                {
                    string absFolder = Application.dataPath + "/" + eventName + "/" + "otherResources" + "/" + "Animation" + "/";

                    string aniPath = "Assets/" + eventName + "/" + "otherResources" + "/" + "Animation" + "/";

                    string assetPath = aniPath + targetClip.name + ".anim";

                    if (!System.IO.Directory.Exists(absFolder))
                    {
                        System.IO.Directory.CreateDirectory(absFolder);
                    }

                    AssetDatabase.CreateAsset(targetClip, assetPath);

                    AssetDatabase.SaveAssets();

                    targetClip = null;
                }
            }

            return targetClip;
        }

        public static void IterateDataClip(Dictionary<string, object> currentInfo, string transformName,string animationProperty, string valueType, int parentStart, Transform root, ref List<List<Keyframe>> keyFrameList)
        {
            int start = -1;

            int end = -1;

            if (currentInfo.ContainsKey("start"))
            {
                start = int.Parse("" + currentInfo["start"]);
            }

            if (currentInfo.ContainsKey("end"))
            {
                end = int.Parse("" + currentInfo["end"]);
            }

            if (currentInfo.ContainsKey("frames"))
            {
                float posScale = 0.01f;

                float timeScale = 30f;
                if (animationProperty.Equals("position"))
                {
                    object getValue = GetValueListByType(currentInfo, "frames", valueType, posScale);

                    List<Vector3> posList = (List<Vector3>) getValue;

                    int endMinus = end - 1;

                    float duration = endMinus - start;

                    float step = duration * 1.0f / posList.Count;

                    if (posList.Count == 1)
                    {
                        for(int i = 0; i < keyFrameList.Count; i++)
                        {
                            keyFrameList[i].Add(new Keyframe(0, posList[0][i]));
                        }

                        float time = (start + step) / timeScale;

                        for (int i = 0; i < keyFrameList.Count; i++)
                        {
                            keyFrameList[i].Add(new Keyframe(time, posList[0][i]));
                        }
                    }
                    else
                    {
                        for(int i = 0; i < posList.Count; i++)
                        {
                            float time = (start + i * step) / timeScale;

                            for (int j = 0; j < keyFrameList.Count; j++)
                            {
                                keyFrameList[j].Add(new Keyframe(time, posList[i][j]));
                            }
                        }
                    }
                }

                if (animationProperty.Equals("quaternion"))
                {
                    object getValue = GetValueListByType(currentInfo, "frames", valueType);

                    List<Vector4> quaList = (List<Vector4>)getValue;

                    int endMinus = end - 1;

                    float duration = endMinus - start;

                    float step = duration * 1.0f / quaList.Count;

                    if (quaList.Count == 1)
                    {
                        for (int i = 0; i < keyFrameList.Count; i++)
                        {
                            keyFrameList[i].Add(new Keyframe(0, quaList[0][i]));
                        }

                        float time = (start + step) / timeScale;

                        for (int i = 0; i < keyFrameList.Count; i++)
                        {
                            keyFrameList[i].Add(new Keyframe(time, quaList[0][i]));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < quaList.Count; i++)
                        {
                            float time = (start + i * step) / timeScale;

                            for (int j = 0; j < keyFrameList.Count; j++)
                            {
                                keyFrameList[j].Add(new Keyframe(time, quaList[i][j]));
                            }
                        }
                    }
                }

            }
        }

        public static MeshRenderer CreateQuadMesh(string id, string eventName, float width, float height, GameObject targetGO, float globalScale)
        {
            float fWidth = width * globalScale;

            float fHeight = height * globalScale;

            MeshRenderer meshRenderer = targetGO.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

            MeshFilter meshFilter = targetGO.AddComponent<MeshFilter>();

            string absFolder = Application.dataPath + "/" + eventName + "/" + "otherResources" + "/" + "Mesh" + "/";

            string absPath = absFolder + id + ".mesh";

            string relativePath = "Assets" + "/" + eventName + "/" + "otherResources" + "/" + "Mesh" + "/" + id + ".mesh";

            Mesh mesh = null;

            //if (!File.Exists(absPath))
            {
                if (!Directory.Exists(absFolder))
                {
                    Directory.CreateDirectory(absFolder);
                }

                mesh = new Mesh();

                Vector3[] vertices = new Vector3[4]
                {
                    new Vector3(-fWidth / 2, -fHeight / 2, 0),
                    new Vector3(fWidth / 2, -fHeight / 2, 0),
                    new Vector3(-fWidth / 2, fHeight / 2, 0),
                    new Vector3(fWidth / 2, fHeight / 2, 0)
                };
                mesh.vertices = vertices;

                int[] tris = new int[6]
                {
                    // lower left triangle
                    0, 2, 1,
                    // upper right triangle
                    2, 3, 1
                };
                mesh.triangles = tris;

                Vector3[] normals = new Vector3[4]
                {
                    -Vector3.forward,
                    -Vector3.forward,
                    -Vector3.forward,
                    -Vector3.forward
                };
                mesh.normals = normals;

                Vector2[] uv = new Vector2[4]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                };
                mesh.uv = uv;

                AssetDatabase.CreateAsset(mesh, relativePath);
            }

            mesh = AssetDatabase.LoadAssetAtPath(relativePath, typeof(Mesh)) as Mesh;

            meshFilter.mesh = mesh;

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            meshRenderer.receiveShadows = false;

            return meshRenderer;
        }

        public static MeshRenderer CreateGeometry(string id, string eventName, DecodeGeometry geo, GameObject targetGO)
        {
            MeshRenderer meshRenderer = targetGO.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

            MeshFilter meshFilter = targetGO.AddComponent<MeshFilter>();

            string absFolder = Application.dataPath + "/" + eventName + "/" + "otherResources" + "/" + "Mesh" + "/";

            string absPath = absFolder + id + ".mesh";

            string relativePath = "Assets" + "/" + eventName + "/" + "otherResources" + "/" + "Mesh" + "/" + id + ".mesh";

            Mesh mesh = null;

            //if (!File.Exists(absPath))
            {
                if (!Directory.Exists(absFolder))
                {
                    Directory.CreateDirectory(absFolder);
                }

                mesh = new Mesh();

                mesh.vertices = geo.vertices.ToArray();

                mesh.triangles = geo.index.ToArray();

                mesh.uv = geo.uv.ToArray();

                mesh.RecalculateNormals();

                AssetDatabase.CreateAsset(mesh, relativePath);
            }

            mesh = AssetDatabase.LoadAssetAtPath(relativePath, typeof(Mesh)) as Mesh;

            meshFilter.mesh = mesh;

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            meshRenderer.receiveShadows = false;

            return meshRenderer;
        }
#endif


        public static object GetSingleValueByType(Dictionary<string,object> origin,string valueName,string type)
        {
            object result = null;

            if (type.Equals("f"))
            {
                return float.Parse("" + origin[valueName]);
            }
            else if (type.Equals("v2"))
            {
                List<object> v2List = (List<object>)origin[valueName];

                for (int i = 0; i < v2List.Count; i += 2)
                {
                    float x = (float)v2List[i];
                    float y = (float)v2List[i + 1];

                    Vector4 newResult = new Vector4(x, y, 0, 0);

                    return newResult;
                }
            }
            else if (type.Equals("v3"))
            {
                List<object> v3List = (List<object>)origin[valueName];

                for (int i = 0; i < v3List.Count; i += 3)
                {
                    float x = (float)v3List[i];
                    float y = (float)v3List[i + 1];
                    float z = (float)v3List[i + 2];

                    Vector4 newResult = new Vector4(x, y, z, 0);

                    return newResult;
                }
            }
            else
            {
                return "" + origin[valueName];
            }
            return result;
        }

        public static object GetValueListByType(Dictionary<string, object> origin, string valueName, string type,float scale = 1)
        {
            List<object> objList = (List<object>)origin[valueName];

            if (type.Equals("f"))
            {
                List<float> floatList = new List<float>();

                foreach(var obj in objList)
                {
                    floatList.Add(float.Parse("" + obj) * scale);
                }

                return floatList;
            }
            else if (type.Equals("v2"))
            {
                List<Vector2> v2List = new List<Vector2>();

                foreach (var obj in objList)
                {
                    List<object> sObjList = (List<object>)obj;

                    for (int i = 0; i < sObjList.Count; i += 2)
                    {
                        float x = (float)sObjList[i];
                        float y = (float)sObjList[i + 1];

                        Vector2 newResult = new Vector2(x, y);

                        newResult *= scale;

                        v2List.Add(newResult);
                    }
                }

                return v2List;
            }
            else if (type.Equals("v3"))
            {
                List<Vector3> v3List = new List<Vector3>();

                foreach (var obj in objList)
                {
                    List<object> sObjList = (List<object>)obj;

                    for (int i = 0; i < sObjList.Count; i += 3)
                    {
                        float x = (float)sObjList[i];
                        float y = (float)sObjList[i + 1];
                        float z = (float)sObjList[i + 2];

                        Vector3 newResult = new Vector3(x, y, z);

                        newResult *= scale;

                        v3List.Add(newResult);
                    }
                }

                return v3List;
            }
            else if (type.Equals("q") || type.Equals("v4"))
            {
                List<Vector4> qList = new List<Vector4>();

                foreach (var obj in objList)
                {
                    List<object> sObjList = (List<object>)obj;

                    for (int i = 0; i < sObjList.Count; i += 4)
                    {
                        float x = (float)sObjList[i];
                        float y = (float)sObjList[i + 1];
                        float z = (float)sObjList[i + 2];
                        float w = (float)sObjList[i + 3];

                        Vector4 newResult = new Vector4(x, y, z, w);

                        newResult *= scale;

                        qList.Add(newResult);
                    }
                }

                return qList;
            }

            return "" + origin[valueName];
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path.Substring(1);
        }

        public static void SetCurveLinear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; ++i)
            {
                float intangent = 0;
                float outtangent = 0;
                bool intangent_set = false;
                bool outtangent_set = false;
                Vector2 point1;
                Vector2 point2;
                Vector2 deltapoint;
                Keyframe key = curve[i];

                if (i == 0)
                {
                    intangent = 0; intangent_set = true;
                }

                if (i == curve.keys.Length - 1)
                {
                    outtangent = 0; outtangent_set = true;
                }

                if (!intangent_set)
                {
                    point1.x = curve.keys[i - 1].time;
                    point1.y = curve.keys[i - 1].value;
                    point2.x = curve.keys[i].time;
                    point2.y = curve.keys[i].value;

                    deltapoint = point2 - point1;

                    intangent = deltapoint.y / deltapoint.x;
                }
                if (!outtangent_set)
                {
                    point1.x = curve.keys[i].time;
                    point1.y = curve.keys[i].value;
                    point2.x = curve.keys[i + 1].time;
                    point2.y = curve.keys[i + 1].value;

                    deltapoint = point2 - point1;

                    outtangent = deltapoint.y / deltapoint.x;
                }

                key.inTangent = intangent;
                key.outTangent = outtangent;
                curve.MoveKey(i, key);
            }
        }

    }
}
