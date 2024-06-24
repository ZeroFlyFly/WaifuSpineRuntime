using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class VendorsDecoder : EditorWindow
{
    public enum ParameterType
    {
        NoImage,
        URL,
        Base64
    }

    public class NetSpineData
    {
        public int manifestID;
        public string relateAllParameterContent;
        public int relateAllParameterIndex;
        public string manifestName;
        public string imgUrl;
        public string atlasString;
        public string jsonString;
        public string jsonUrl;

        public ParameterType imageType = ParameterType.NoImage;
    }

    string inputUrl = "";

    [MenuItem("ZeroFly/Read Vendors Content")]
    static void OpenDecoderWindow()
    {
        EditorWindow.GetWindow(typeof(VendorsDecoder), false, "Vendors Decoder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Vendors Decoder Window", EditorStyles.boldLabel);

        //EditorGUI.BeginChangeCheck();
        inputUrl = EditorGUILayout.TextField("Web Url: ", inputUrl, EditorStyles.textField);

        inputUrl = inputUrl.Trim();

        bool isValid = true;

        //if (EditorGUI.EndChangeCheck())
        {
            // the value has changed
            if (string.IsNullOrEmpty(inputUrl))
            {
                GUILayout.Label("Please Input Event Index Url", EditorStyles.boldLabel);

                isValid = false;
            }
            else if (!inputUrl.Contains("https://"))
            {
                GUILayout.Label("Error! No Https Header Found", EditorStyles.boldLabel);

                isValid = false;
            }
            else if (!inputUrl.ToLower().Contains("mihoyo") && !inputUrl.ToLower().Contains("hoyoverse"))
            {
                GUILayout.Label("Error! Does not looks like a mihoyo link", EditorStyles.boldLabel);

                isValid = false;
            }
        }

        if (GUILayout.Button("Start Decode"))
        {
            if (isValid)
            {
                DecodeVendors(inputUrl);
            }
            else
            {
                UnityEngine.Debug.LogError("No Valid Input Url Detect!");
            }
            //this.Close();
        }

        if (isValid)
        {
            int zoneUrlStartIndex = inputUrl.IndexOf("//");

            zoneUrlStartIndex += 2;

            int zoneUrlEndIndex = inputUrl.IndexOf("/", zoneUrlStartIndex);

            string zoneUrl = inputUrl;

            if (zoneUrlEndIndex > 0)
            {
                zoneUrl = inputUrl.Substring(0, zoneUrlEndIndex);

                string[] eventLinkSplit = inputUrl.Split('?');

                string eventUrlLink = eventLinkSplit[0];

                string[] eventPathList = eventUrlLink.Split('/');

                GUILayout.Label("目前检测到的信息 :", EditorStyles.boldLabel);

                GUILayout.Label("域名 : " + zoneUrl, EditorStyles.boldLabel);

                if ((eventPathList.Length - 2) != 2)
                {
                    string folderSubPath = eventPathList[eventPathList.Length - 2];

                    GUILayout.Label("网页活动名 : " + folderSubPath, EditorStyles.boldLabel);
                }

                GUILayout.Label("网页有效链接 : " + eventUrlLink, EditorStyles.boldLabel);

                GUILayout.Label("请检查上述信息是否正确~", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("目前检测到的信息 :", EditorStyles.boldLabel);

                GUILayout.Label("域名 : " + zoneUrl, EditorStyles.boldLabel);

                GUILayout.Label("请检查上述信息是否正确~", EditorStyles.boldLabel);
            }
        }
        else
        {
            GUILayout.Label("Example : " + "https://act.mihoyo.com/ys/event/e20231209preview-yh731z/index.html", EditorStyles.boldLabel);
        }
    }

    public static string FormatJson(string json)
    {
        //单引号替换成双引号
        json = json.Replace("'", "\"");
  
        /* 正则表达式说明
        ([{]|[,]) 包含 { 或 , 字符 $1
        ([\s]*) 包含0或多个空字符 $2
        ([\w]+?) 包含a-zA-Z0-9_等字符的属性，配置多次 $3
        ([\s]*) $4
        ([:]) 后面包含 : 字符 $5
        */
        Regex regex = new Regex(@"([{]|[,])([\s]*)([\w]+?)([\s]*)([:])", RegexOptions.Multiline);

        //替换符合该正则表达式的内容 保留1、3、5项，并在中间加上双引号
        json = regex.Replace(json, "$1\"$3\"$5");

        Regex regex2 = new Regex(@"([^(0-9)])([.])([(0-9)]+)", RegexOptions.Multiline);

        json = regex2.Replace(json, "${1}0${2}${3}");

        json = json.Replace("!0", "true");

        json = json.Replace("!1", "false");

        return json;
    }

    static void DecodeVendors(string inputUrlLink)
    {
        #region Decode URL Find Vendor And Index
        //替换成活动链接,注意是Vendors所在的路径，不是网页路径
        //string urlParent = "https://act.mihoyo.com/act/ys/event/e20230928review/";


        //string inputUrlLink = "https://act.mihoyo.com/ys/event/e20231209preview-yh731z/index.html?game_biz=hk4e_cn&mhy_presentation_style=fullscreen&mhy_auth_required=true&mhy_landscape=true&mhy_hide_status_bar=true&mode=fullscreen";

        var watch = Stopwatch.StartNew();

        string[] eventLinkSplit = inputUrlLink.Split('?');

        string eventUrlLink = eventLinkSplit[0];

        string[] eventPathList = eventUrlLink.Split('/');

        //string folderSubPath = eventPathList[eventPathList.Length - 2];

        string folderSubPath = "";

        foreach (var str in eventPathList)
        {
            if (!string.IsNullOrEmpty(folderSubPath))
            {
                continue;
            }

            if (str.StartsWith("e") && str.Length > 1)
            {
                string s1 = "" + str[1];

                int number = 0;

                bool isNumber = int.TryParse(s1, out number);

                if (isNumber)
                {
                    folderSubPath = str;

                    break;
                }
            }
        }

        string urlParent = "";

        string vendorsUrl = "";

        string vendorsFileLocalPath = "";

        string vendorsFileName = "";

        string indexUrl = "";

        string indexFileLocalPath = "";

        string indexFileName = "";


        if (!string.IsNullOrEmpty(folderSubPath))
        {
            string configPath = Application.dataPath + "/" + folderSubPath + "/";

            string configName = configPath + folderSubPath + "_Config.txt";

            if (Directory.Exists(configPath) && File.Exists(configName))
            {
                string configContent = File.ReadAllText(configName, Encoding.UTF8);

                string[] configList = configContent.Split('\n');

                if (configList.Length > 5)
                {
                    urlParent = configList[0];

                    vendorsFileName = configList[1];

                    vendorsUrl = configList[2];

                    indexFileName = configList[3];

                    indexUrl = configList[4];
                }
            }
        }

        if (string.IsNullOrEmpty(vendorsUrl))
        {
            int zoneUrlStartIndex = inputUrlLink.IndexOf("//");

            zoneUrlStartIndex += 2;

            int zoneUrlEndIndex = inputUrlLink.IndexOf("/", zoneUrlStartIndex);

            string zoneUrl = zoneUrlEndIndex > 0 ? inputUrlLink.Substring(0, zoneUrlEndIndex) : inputUrlLink;

            int lastEventUrlSlashPos = eventUrlLink.LastIndexOf("/");

            string indexRootUrl = eventUrlLink.Substring(0, lastEventUrlSlashPos);

            string[] indexRootUrlList = indexRootUrl.Split('/');

            string vendorsJSPattern = "vendors";

            using (WebClient client = new WebClient())
            {
                client.Proxy = null;

                string webHtmlContent = client.DownloadString(new Uri(eventUrlLink));

                foreach (Match perLineMatch in Regex.Matches(webHtmlContent, vendorsJSPattern))
                {
                    int detectIndex = GetStringIndexBackward(webHtmlContent, perLineMatch.Index, new char[] { '\"' });

                    string vendorsSubUrl = FindPairComment(detectIndex, webHtmlContent, false);

                    if (vendorsSubUrl.EndsWith(".js"))
                    {
                        string[] vendorsSubUrlList = vendorsSubUrl.Split('/');

                        folderSubPath = vendorsSubUrlList.Length > 2 ? vendorsSubUrlList[vendorsSubUrlList.Length - 2] : indexRootUrlList[indexRootUrlList.Length - 1];

                        vendorsUrl = vendorsSubUrl.StartsWith("https://") ? vendorsSubUrl : vendorsSubUrl.StartsWith("/") ? (zoneUrl + vendorsSubUrl) : (indexRootUrl + "/" + vendorsSubUrl);

                        int backSlashPos = vendorsUrl.LastIndexOf('/');

                        urlParent = vendorsUrl.Substring(0, backSlashPos + 1);

                        vendorsFileName = vendorsUrl.Substring(backSlashPos + 1);

                        #region Find Index URL

                        bool hasIndexFile = false;

                        string indexJSPattern = "index";

                        foreach (Match perIndexMatch in Regex.Matches(webHtmlContent, indexJSPattern))
                        {
                            int detectJSStartIndex = GetStringIndexBackward(webHtmlContent, perIndexMatch.Index, new char[] { '\"' });

                            string indexJSSubUrl = FindPairComment(detectJSStartIndex, webHtmlContent, false);

                            if (indexJSSubUrl.EndsWith(".js"))
                            {
                                string[] indexJSSubUrlList = indexJSSubUrl.Split('/');

                                indexUrl = indexJSSubUrl.StartsWith("https://") ? indexJSSubUrl : indexJSSubUrl.StartsWith("/") ? (zoneUrl + indexJSSubUrl) : (indexRootUrl + "/" + indexJSSubUrl);

                                int indexBackSlashPos = indexUrl.LastIndexOf('/');

                                indexFileName = indexUrl.Substring(indexBackSlashPos + 1);

                                hasIndexFile = true;

                                break;
                            }
                        }

                        if (!hasIndexFile) // In old website, it names bundle
                        {
                            indexJSPattern = "bundle";

                            foreach (Match perIndexMatch in Regex.Matches(webHtmlContent, indexJSPattern))
                            {
                                int detectJSStartIndex = GetStringIndexBackward(webHtmlContent, perIndexMatch.Index, new char[] { '\"' });

                                string indexJSSubUrl = FindPairComment(detectJSStartIndex, webHtmlContent, false);

                                if (indexJSSubUrl.EndsWith(".js"))
                                {
                                    string[] indexJSSubUrlList = indexJSSubUrl.Split('/');

                                    indexUrl = indexJSSubUrl.StartsWith("https://") ? indexJSSubUrl : indexJSSubUrl.StartsWith("/") ? (zoneUrl + indexJSSubUrl) : (indexRootUrl + "/" + indexJSSubUrl);

                                    int indexBackSlashPos = indexUrl.LastIndexOf('/');

                                    indexFileName = indexUrl.Substring(indexBackSlashPos + 1);

                                    hasIndexFile = true;

                                    break;
                                }
                            }
                        }


                        #endregion


                        string configPath = Application.dataPath + "/" + folderSubPath + "/";

                        string configName = configPath + folderSubPath + "_Config.txt";

                        if (!string.IsNullOrEmpty(urlParent) && !string.IsNullOrEmpty(vendorsFileName))
                        {
                            if (!Directory.Exists(configPath))
                            {
                                Directory.CreateDirectory(configPath);
                            }

                            string writeContent = urlParent + "\n" + vendorsFileName + "\n" + vendorsUrl + "\n" + indexFileName + "\n" + indexUrl + "\n" + eventUrlLink;

                            File.WriteAllText(configName, writeContent);
                        }

                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(vendorsFileName))
        {
            UnityEngine.Debug.LogError("No Valid Vendors Url Found! Failed!");
            return;
        }

        vendorsFileLocalPath = Application.dataPath + "/" + folderSubPath + "/" + vendorsFileName;

        indexFileLocalPath = Application.dataPath + "/" + folderSubPath + "/" + indexFileName;

        if (!File.Exists(vendorsFileLocalPath))
        {
            using (WebClient client = new WebClient())
            {
                client.Proxy = null;

                client.DownloadFile(new Uri(vendorsUrl), vendorsFileLocalPath);
            }
        }

        if (!File.Exists(indexFileLocalPath))
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Proxy = null;

                    client.DownloadFile(new Uri(indexUrl), indexFileLocalPath);
                }
            }
            catch(Exception e)
            {
                UnityEngine.Debug.Log("Can not Get Index File");
            }
            
        }

        if (!File.Exists(vendorsFileLocalPath))
        {
            UnityEngine.Debug.LogError("No Js File Found. Please Check vendorsFileLocalPath");
            return;
        }

        string vendorsText = File.ReadAllText(vendorsFileLocalPath, Encoding.UTF8);

        vendorsText = vendorsText.Trim();

        #endregion

        #region Decide Function Names

        string firstFuncName = "";

        string secondFuncName = "";

        string thirdFuncName = "";

        string findFuncPattern = "function\\((.+?)\\)";

        Dictionary<string, int> firstNameCount = new Dictionary<string, int>();

        Dictionary<string, int> secondNameCount = new Dictionary<string, int>();

        Dictionary<string, int> thirdNameCount = new Dictionary<string, int>();

        foreach (Match funcPattern in Regex.Matches(vendorsText, findFuncPattern))
        {
            int funcStartIndex = funcPattern.Index + 8;

            string contentInBracket = FindPairBracket(funcStartIndex, vendorsText, false);

            string[] contentInBracketSplit = contentInBracket.Split(',');

            if (contentInBracketSplit.Length > 0)
            {
                string functionName1 = contentInBracketSplit[0];

                if (firstNameCount.ContainsKey(functionName1))
                {
                    firstNameCount[functionName1] = firstNameCount[functionName1] + 1;
                }
                else
                {
                    firstNameCount.Add(functionName1, 1);
                }
            }

            if (contentInBracketSplit.Length > 1)
            {
                string functionName2 = contentInBracketSplit[1];

                if (secondNameCount.ContainsKey(functionName2))
                {
                    secondNameCount[functionName2] = secondNameCount[functionName2] + 1;
                }
                else
                {
                    secondNameCount.Add(functionName2, 1);
                }
            }

            if (contentInBracketSplit.Length > 2)
            {
                string functionName3 = contentInBracketSplit[2];

                if (thirdNameCount.ContainsKey(functionName3))
                {
                    thirdNameCount[functionName3] = thirdNameCount[functionName3] + 1;
                }
                else
                {
                    thirdNameCount.Add(functionName3, 1);
                }
            }
        }

        firstFuncName = FindMostPopString(firstNameCount);

        secondFuncName = FindMostPopString(secondNameCount);

        thirdFuncName = FindMostPopString(thirdNameCount);

        if (string.IsNullOrEmpty(firstFuncName)
            || string.IsNullOrEmpty(secondFuncName)
              || string.IsNullOrEmpty(thirdFuncName))
        {
            UnityEngine.Debug.LogError("Can not Decide Function Name");

            return;
        }

        #endregion

        #region Get All ResourcesID in vendors

        Dictionary<string, string> ALREADY_USE_VENDORS_RESOURCES = new Dictionary<string, string>();

        Dictionary<string, string> ALREADY_USE_INDEX_RESOURCES = new Dictionary<string, string>();

        Dictionary<string, string> VENDOR_ID_TO_RESOURCE = new Dictionary<string, string>();

        string exportPattern = ".exports=";

        foreach (Match exportParam in Regex.Matches(vendorsText, exportPattern))
        {
            int exportIndex = exportParam.Index;

            int numberEndPos = GetStringIndexBackward(vendorsText, exportIndex, new char[] { ':' });

            int numberStartPos = GetStringIndexBackward(vendorsText, numberEndPos, new char[] { '{', ',' }) + 1;

            int length = numberEndPos - numberStartPos;

            if (length > 0)
            {
                string vendorsSubString = vendorsText.Substring(numberStartPos, length);

                vendorsSubString = TrimQuote(vendorsSubString, '\"');

                if (IsValidPrefix(vendorsSubString))
                {
                    int contentIndex = GetStringIndexForward(vendorsText, exportIndex, new char[] { '"' });

                    string content = FindPairComment(contentIndex, vendorsText, false);

                    if (!string.IsNullOrEmpty(content))
                    {
                        if (IsResources(content))
                        {
                            /*if (content.EndsWith(".bin"))
                            {
                                if (!OTHER_SOURCES_OF_INTEREST.ContainsKey(vendorsSubString))
                                    OTHER_SOURCES_OF_INTEREST.Add(vendorsSubString, content);
                            }
                            else*/
                            {
                                //UnityEngine.Debug.Log("Vendors " + vendorsSubString + " : " + content);
                                if (!VENDOR_ID_TO_RESOURCE.ContainsKey(vendorsSubString))
                                    VENDOR_ID_TO_RESOURCE.Add(vendorsSubString, content);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Get All ResourcesID in index

        Dictionary<string, string> INDEX_ID_TO_RESOURCE = new Dictionary<string, string>();

        if (File.Exists(indexFileLocalPath))
        {
            string indexText = File.ReadAllText(indexFileLocalPath, Encoding.UTF8);

            indexText = indexText.Trim();

            foreach (Match exportParam in Regex.Matches(indexText, exportPattern))
            {
                int exportIndex = exportParam.Index;

                int numberEndPos = GetStringIndexBackward(indexText, exportIndex, new char[] { ':' });

                int numberStartPos = GetStringIndexBackward(indexText, numberEndPos, new char[] { '{', ',' }) + 1;

                int length = numberEndPos - numberStartPos;

                if (length > 0)
                {
                    string indexIDSubString = indexText.Substring(numberStartPos, length);

                    indexIDSubString = TrimQuote(indexIDSubString, '\"');

                    if (IsValidPrefix(indexIDSubString))
                    {
                        int contentIndex = GetStringIndexForward(indexText, exportIndex, new char[] { '"' });

                        string content = FindPairComment(contentIndex, indexText, false);

                        if (!string.IsNullOrEmpty(content))
                        {
                            if (IsResources(content))
                            {
                                /*if (content.EndsWith(".bin"))
                                {
                                    if (!OTHER_SOURCES_OF_INTEREST.ContainsKey(indexIDSubString))
                                        OTHER_SOURCES_OF_INTEREST.Add(indexIDSubString, content);
                                }
                                else*/
                                {
                                    //UnityEngine.Debug.Log("Index " + indexIDSubString + " : " + content);
                                    if (!INDEX_ID_TO_RESOURCE.ContainsKey(indexIDSubString))
                                        INDEX_ID_TO_RESOURCE.Add(indexIDSubString, content);
                                }

                            }
                        }
                    }
                }
            }
        }
        

        #endregion

        //// -------------------------  START MANIFEST STYLE WEBSITE  -------------------------------
        #region Decode ALL Parameter

        List<string> ALL_PARAMETER = new List<string>();

        string allNBrackets = thirdFuncName + "\\(().+?\\)";

        int lastBracketIndex = 0;

        List<List<string>> bracketRelation = new List<List<string>>();

        List<string> currentBracketList = new List<string>();

        foreach (Match matchParam in Regex.Matches(vendorsText, allNBrackets))
        {
            if (lastBracketIndex == 0)
            {
                currentBracketList.Add(matchParam.Value);

                lastBracketIndex = matchParam.Index;

                continue;
            }

            if ((matchParam.Index - lastBracketIndex) < 16)
            {
                currentBracketList.Add(matchParam.Value);

                lastBracketIndex = matchParam.Index;
            }
            else
            {
                bracketRelation.Add(currentBracketList);

                currentBracketList = new List<string>();

                currentBracketList.Add(matchParam.Value);

                lastBracketIndex = matchParam.Index;
            }
        }

        int maxBracketHitCount = 0;

        int maxBracketIndex = -1;

        for (int i = 0; i < bracketRelation.Count; i++)
        {
            int currentHitCount = 0;

            foreach(var tag in bracketRelation[i])
            {
                string key = GetContentByTag(tag, thirdFuncName);

                if(INDEX_ID_TO_RESOURCE.ContainsKey(key) || VENDOR_ID_TO_RESOURCE.ContainsKey(key))
                {
                    currentHitCount += 1;
                }
            }
            
            if (currentHitCount > maxBracketHitCount)
            {
                maxBracketIndex = i;

                maxBracketHitCount = currentHitCount;
            }
        }

        if (maxBracketIndex >= 0)
        {

            foreach (var pair in bracketRelation[maxBracketIndex])
            {
                string value = pair;

                if (value.Contains("(") && value.Contains(")"))
                {
                    int startValueIndex = value.IndexOf('(');

                    int endValueIndex = value.LastIndexOf(')');

                    int valueLength = endValueIndex - startValueIndex - 1;

                    if ((startValueIndex + 1) < value.Length && valueLength > 0)
                    {
                        value = value.Substring(startValueIndex + 1, valueLength);

                        if (!value.Contains("(") && !value.Contains(")") && !value.Contains(" ") && !value.Contains("?"))
                        {
                            value = TrimQuote(value, '\"');

                            value = TrimQuote(value, '\'');

                            if (IsValidPrefix(value))
                            {
                                ALL_PARAMETER.Add(value);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            UnityEngine.Debug.LogError("No Parameter Found! Check Context");
            return;
        }

        /*for(int i = 0; i < ALL_PARAMETER.Count; i++)
        {
            UnityEngine.Debug.Log("ALL" + ALL_PARAMETER[i]);
        }*/

        #endregion

        #region Get MANIFEST

        Dictionary<int, NetSpineData> MANIFEST_ID_TO_NAME = new Dictionary<int, NetSpineData>();

        Dictionary<string, List<NetSpineData>> NAME_TO_MANIFESTLIST = new Dictionary<string, List<NetSpineData>>();

        List<int> jsonManifestIDList = new List<int>();

        string manifestPattern = "_MANIFEST=";

        MatchCollection manifestCollect = Regex.Matches(vendorsText, manifestPattern);

        //List<Match> jsonAndAtlasCollection = new List<Match>();

        foreach (Match manifestMatch in manifestCollect)
        {
            int bracketStartIndex = manifestMatch.Index + 10;

            string contentBetween = FindPairBracket(bracketStartIndex, vendorsText, true);

            string contentBetweenBigBracket = "\\{(.+?)\\}";
            foreach (Match perLineMatch in Regex.Matches(contentBetween, contentBetweenBigBracket))
            {
                int PAIR_ID = -1;
                string NAME_ID = "";

                string perLineMatchValue = perLineMatch.Value;

                string srcnPattern = "src:" + thirdFuncName;

                int SRC_N_Index = perLineMatchValue.IndexOf(srcnPattern);

                if (SRC_N_Index >= 0)
                {
                    SRC_N_Index += srcnPattern.Length;

                    string s_ID = FindPairBracket(SRC_N_Index, perLineMatchValue, false);

                    var pair = ConvertJSNumber(s_ID);

                    PAIR_ID = pair.Item2;
                }

                int Name_Index = perLineMatchValue.IndexOf("id:");

                if (Name_Index >= 0)
                {
                    Name_Index += 3;

                    NAME_ID = FindPairComment(Name_Index, perLineMatchValue, false);
                }

                if (PAIR_ID >= 0)
                {
                    if (!MANIFEST_ID_TO_NAME.ContainsKey(PAIR_ID))
                    {
                        NetSpineData avalableData = new NetSpineData();

                        avalableData.manifestID = PAIR_ID;

                        avalableData.manifestName = NAME_ID;

                        MANIFEST_ID_TO_NAME.Add(PAIR_ID, avalableData);

                        if (NAME_TO_MANIFESTLIST.ContainsKey(NAME_ID))
                        {
                            List<NetSpineData> list = NAME_TO_MANIFESTLIST[NAME_ID];

                            list.Add(avalableData);
                        }
                        else
                        {
                            List<NetSpineData> list = new List<NetSpineData>();

                            list.Add(avalableData);

                            NAME_TO_MANIFESTLIST.Add(NAME_ID, list);
                        }
                    }
                }
                else
                {
                    if (perLineMatchValue.Contains("atlas") && perLineMatchValue.Contains("json"))
                    {
                        int jsonID = GetNumberByTag(perLineMatchValue, "json");
                        jsonManifestIDList.Add(jsonID);
                    }
                }
            }
        }

        #endregion

        /*foreach(var pair in MANIFEST_ID_TO_NAME)
        {
            UnityEngine.Debug.Log("AA " + pair.Key + " " + pair.Value);
        }*/

        #region Try Calc ALL PARAMETER OFFSET

        int latestHitIndex = -1;

        int latestManifestID = -1;

        for (int i = 0; i < ALL_PARAMETER.Count; i++)
        {
            string sPrefix = ALL_PARAMETER[i];

            string contentBetween = "";// FindPairComment(bracketStartIndex, vendorsText, false);

            if (string.IsNullOrEmpty(contentBetween))
            {
                if (INDEX_ID_TO_RESOURCE.ContainsKey(sPrefix))
                {
                    string toCheck = INDEX_ID_TO_RESOURCE[sPrefix];

                    if (toCheck.EndsWith(".png") || toCheck.EndsWith(".jpg"))
                    {
                        contentBetween = INDEX_ID_TO_RESOURCE[sPrefix];

                        if(!ALREADY_USE_INDEX_RESOURCES.ContainsKey(sPrefix))
                            ALREADY_USE_INDEX_RESOURCES.Add(sPrefix, "1");
                    }
                }
            }

            if (string.IsNullOrEmpty(contentBetween))
            {
                if (VENDOR_ID_TO_RESOURCE.ContainsKey(sPrefix))
                {
                    string toCheck = VENDOR_ID_TO_RESOURCE[sPrefix];

                    if (toCheck.EndsWith(".png") || toCheck.EndsWith(".jpg"))
                    {
                        contentBetween = VENDOR_ID_TO_RESOURCE[sPrefix];

                        if (!ALREADY_USE_VENDORS_RESOURCES.ContainsKey(sPrefix))
                            ALREADY_USE_VENDORS_RESOURCES.Add(sPrefix, "1");
                    }
                }
            }

            if (string.IsNullOrEmpty(contentBetween))
            {
                string prefix = "" + ALL_PARAMETER[i];

                prefix = ConvertToValidPrefix(prefix);

                string matchPattern = prefix + ":function\\(" + firstFuncName + "," + secondFuncName + "," + thirdFuncName + "\\)\\{\"use strict\";" + firstFuncName + ".exports=" + thirdFuncName + ".p\\+";

                foreach (Match perLineMatch in Regex.Matches(vendorsText, matchPattern))
                {
                    int findColonIndex = matchPattern.IndexOf(':') - 1;

                    string doubleCheckPrefix = GetStringBackward(vendorsText, perLineMatch.Index + findColonIndex, new char[] { ',', '{' });

                    if (doubleCheckPrefix.Equals(prefix))
                    {
                        int bracketStartIndex = perLineMatch.Index + perLineMatch.Value.Length;

                        contentBetween = FindPairComment(bracketStartIndex, vendorsText, false);

                        if (!string.IsNullOrEmpty(contentBetween))
                        {
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(contentBetween))
            {
                string id_name = GetFileName(contentBetween);

                if (NAME_TO_MANIFESTLIST.ContainsKey(id_name))
                {
                    latestHitIndex = i;

                    if (NAME_TO_MANIFESTLIST[id_name][0].imageType == ParameterType.Base64 || NAME_TO_MANIFESTLIST[id_name][0].imageType == ParameterType.URL)
                        continue;

                    int relateManifestID = -1;

                    // Find Hit Parameter. Manage All The Base64 Before
                    foreach (var data in NAME_TO_MANIFESTLIST[id_name])
                    {
                        data.imgUrl = contentBetween;

                        data.imageType = ParameterType.URL;

                        data.relateAllParameterContent = ALL_PARAMETER[i];

                        //Important

                        data.relateAllParameterIndex = i;

                        relateManifestID = data.manifestID;

                        //Important
                        latestManifestID = relateManifestID;
                        break;
                    }

                    int nextManifestID = latestManifestID - 1;

                    for (int j = i - 1; j > 0; j--)
                    {
                        if (jsonManifestIDList.Contains(nextManifestID))
                        {
                            nextManifestID--;
                        }

                        if (MANIFEST_ID_TO_NAME.ContainsKey(nextManifestID))
                        {
                            if (MANIFEST_ID_TO_NAME[nextManifestID].imageType == ParameterType.URL)
                            {
                                break;
                            }
                        }
                        else
                        {
                            nextManifestID--;
                            continue;
                        }

                        bool hasEncodeData = DecideBase64Case(ALL_PARAMETER, MANIFEST_ID_TO_NAME, j, nextManifestID, vendorsText, INDEX_ID_TO_RESOURCE, VENDOR_ID_TO_RESOURCE);

                        nextManifestID--;
                    }
                }
            }

            //if (hasCheck)
            //    continue;
        }

        int followManifestID = latestHitIndex + 1;
        // Avoid Following All Base 64 case
        for (int i = (latestHitIndex + 1); i < ALL_PARAMETER.Count; i++)
        {
            if (jsonManifestIDList.Contains(followManifestID))
            {
                followManifestID += 1;
            }
            DecideBase64Case(ALL_PARAMETER, MANIFEST_ID_TO_NAME, i, followManifestID, vendorsText, INDEX_ID_TO_RESOURCE, VENDOR_ID_TO_RESOURCE);

            followManifestID += 1;
        }

        #endregion

        /*foreach(var pair in MANIFEST_ID_TO_NAME)
        {
            if(pair.Value.imageType != ParameterType.NoImage)
                UnityEngine.Debug.Log(pair.Key + " : " + pair.Value.imgUrl + " : " + pair.Value.manifestName);
        }*/

        //return;
        #region Detect JSON ID

        int lastSRCIndex = 0;

        foreach (Match manifestMatch in manifestCollect)
        {
            int bracketStartIndex = manifestMatch.Index + 10;

            string contentBetween = FindPairBracket(bracketStartIndex, vendorsText, true);

            string contentBetweenBigBracket = "\\{(.+?)\\}";

            foreach (Match perLineMatch in Regex.Matches(contentBetween, contentBetweenBigBracket))
            {
                string perLineMatchValue = perLineMatch.Value;

                string srcnPattern = "src:" + thirdFuncName;

                int SRC_N_Index = perLineMatchValue.IndexOf(srcnPattern);

                if (SRC_N_Index < 0)
                {
                    //Get Atlas And Json
                    if (perLineMatchValue.Contains("atlas") && perLineMatchValue.Contains("json"))
                    {
                        bool alreadyHasID = perLineMatchValue.Contains(":{");

                        int startIndex = alreadyHasID ? (perLineMatch.Index + perLineMatchValue.IndexOf(":{")) - 1 : perLineMatch.Index - 2;
                        startIndex += bracketStartIndex;

                        string currentID = GetStringBackward(vendorsText, startIndex, new char[] { ',', '{' });

                        if (NAME_TO_MANIFESTLIST.ContainsKey(currentID))
                        {
                            //int atlasID = GetNumberByTag(perLineMatchValue, "atlas");
                            int jsonID = GetNumberByTag(perLineMatchValue, "json");

                            int START_PARAM_ID = MANIFEST_ID_TO_NAME[lastSRCIndex].relateAllParameterIndex;

                            int JSON_PARAMETER_INDEX = START_PARAM_ID + (jsonID - lastSRCIndex) / 2;

                            if (JSON_PARAMETER_INDEX < ALL_PARAMETER.Count && JSON_PARAMETER_INDEX >= 0)
                            {
                                string sJsonPrefix = ALL_PARAMETER[JSON_PARAMETER_INDEX];

                                string jsonContentBetween = "";

                                if (string.IsNullOrEmpty(jsonContentBetween))
                                {
                                    if (INDEX_ID_TO_RESOURCE.ContainsKey(sJsonPrefix))
                                    {
                                        string toCheck = INDEX_ID_TO_RESOURCE[sJsonPrefix];
                                        if (toCheck.EndsWith(".json"))
                                        {
                                            jsonContentBetween = INDEX_ID_TO_RESOURCE[sJsonPrefix];

                                            if (!ALREADY_USE_INDEX_RESOURCES.ContainsKey(sJsonPrefix))
                                                ALREADY_USE_INDEX_RESOURCES.Add(sJsonPrefix, "1");
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(jsonContentBetween))
                                {
                                    if (VENDOR_ID_TO_RESOURCE.ContainsKey(sJsonPrefix))
                                    {
                                        string toCheck = VENDOR_ID_TO_RESOURCE[sJsonPrefix];
                                        if (toCheck.EndsWith(".json"))
                                        {
                                            jsonContentBetween = VENDOR_ID_TO_RESOURCE[sJsonPrefix];

                                            if(!ALREADY_USE_VENDORS_RESOURCES.ContainsKey(sJsonPrefix))
                                                ALREADY_USE_VENDORS_RESOURCES.Add(sJsonPrefix, "1");
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(jsonContentBetween))
                                {
                                    string jsonPrefix = "" + sJsonPrefix;

                                    string jsonMatchPattern = jsonPrefix + ":function\\(" + firstFuncName + "," + secondFuncName + "," + thirdFuncName + "\\)\\{" + firstFuncName + ".exports=" + thirdFuncName + ".p\\+";

                                    foreach (Match jsonPerLineMatch in Regex.Matches(vendorsText, jsonMatchPattern))
                                    {
                                        int findColonIndex = jsonMatchPattern.IndexOf(':') - 1;

                                        string doubleCheckPrefix = GetStringBackward(vendorsText, perLineMatch.Index + findColonIndex, new char[] { ',', '{' });

                                        if (doubleCheckPrefix.Equals(jsonPrefix))
                                        {
                                            int jsonBracketStartIndex = jsonPerLineMatch.Index + jsonPerLineMatch.Value.Length;

                                            jsonContentBetween = FindPairComment(jsonBracketStartIndex, vendorsText, false);

                                            break;
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(jsonContentBetween))
                                {
                                    string jsonName = GetFileName(jsonContentBetween);

                                    if (NAME_TO_MANIFESTLIST.ContainsKey(jsonName))
                                    {
                                        foreach (var data in NAME_TO_MANIFESTLIST[jsonName])
                                        {
                                            data.jsonUrl = jsonContentBetween;

                                            break;
                                        }
                                    }
                                    else
                                    {
                                        NetSpineData data = new NetSpineData();

                                        data.jsonUrl = jsonContentBetween;

                                        NAME_TO_MANIFESTLIST.Add(jsonName, new List<NetSpineData>() { data });

                                        UnityEngine.Debug.Log("MISSING JSON " + jsonName);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    int Name_Index = perLineMatchValue.IndexOf("id:");

                    if (Name_Index >= 0)
                    {
                        Name_Index += 3;

                        string NAME_ID = FindPairComment(Name_Index, perLineMatchValue, false);

                        if (NAME_TO_MANIFESTLIST.ContainsKey(NAME_ID))
                        {
                            lastSRCIndex = NAME_TO_MANIFESTLIST[NAME_ID][0].manifestID;
                        }
                    }
                }
            }
        }

        #endregion

        #region Detect ATLAS

        List<string> atlasOrder = new List<string>();

        string AtlasPattern = "(function\\(" + firstFuncName + "," + secondFuncName + "\\){" + firstFuncName + ".exports=[\"])(?:\"|.)*?[\"]";

        foreach (Match match in Regex.Matches(vendorsText, AtlasPattern))
        {
            #region analyse atlas

            string rawAtlasMatch = match.Value;

            if (rawAtlasMatch.Contains(".png"))
            {
                int imageIndex = rawAtlasMatch.IndexOf(".png");

                int startIndex = rawAtlasMatch.IndexOf('"') + 1;

                string bestMatchName = rawAtlasMatch.Substring(startIndex, imageIndex - startIndex);

                string imageName = rawAtlasMatch.Substring(startIndex, imageIndex + 4 - startIndex);

                imageName = GetFileName(imageName);

                string atlas = rawAtlasMatch.Substring(startIndex, rawAtlasMatch.Length - startIndex - 1);

                atlas = atlas.Replace("\\n", "\n");

                atlas = atlas.Replace("\\t", "");

                if (NAME_TO_MANIFESTLIST.ContainsKey(imageName))
                {
                    foreach (var data in NAME_TO_MANIFESTLIST[imageName])
                    {
                        data.atlasString = atlas;

                        break;
                    }
                }
                else
                {
                    NetSpineData data = new NetSpineData();

                    data.atlasString = atlas;

                    NAME_TO_MANIFESTLIST.Add(imageName, new List<NetSpineData>() { data });

                    UnityEngine.Debug.Log("MISSING : Atlas " + imageName + "  has no relate resources.");
                }

                atlasOrder.Add(imageName);

            }
            #endregion
        }

        bool needToAssignJson = false;

        foreach (var s in atlasOrder)
        {
            string jsonURL = NAME_TO_MANIFESTLIST[s][0].jsonUrl;

            string jsonString = NAME_TO_MANIFESTLIST[s][0].jsonString;

            if (string.IsNullOrEmpty(jsonURL) && string.IsNullOrEmpty(jsonString))
            {
                needToAssignJson = true;

                UnityEngine.Debug.Log("Need To Assign Json " + needToAssignJson);

                break;
            }
        }

        if (needToAssignJson)
        {
            string JsonPattern = "JSON.parse\\(\'{\"skeleton\"";

            var JSONMatch = Regex.Matches(vendorsText, JsonPattern);

            for (int i = 0; i < JSONMatch.Count; i++)
            {
                Match jsonM = JSONMatch[i];

                int startIndex = jsonM.Index + 11;

                string json = FindJsonPairComment(startIndex, vendorsText, false);

                json = json.Replace("4.0-from-", "");

                json = json.Replace("4.1-from-", "");

                string s = atlasOrder[i];

                NAME_TO_MANIFESTLIST[s][0].jsonString = json;
            }
        }
        #endregion

        //// -------------------------  END MANIFEST STYLE WEBSITE  -------------------------------

        //// -------------------------  START OBJECT ASSIGN WEBSITE  -------------------------------

        #region Ddtect Object Assign WebSite

        if(MANIFEST_ID_TO_NAME.Count == 0)
        {
            // Oh no manifest decode failed

            // First, Analysis Resource Path
            Dictionary<string, string> IMG_NAME_TO_URL = new Dictionary<string, string>();

            string imageURLPattern = thirdFuncName + ".p\\+";

            foreach (Match imageURL in Regex.Matches(vendorsText, imageURLPattern))
            {
                int imageURLStartIndex = imageURL.Index + thirdFuncName.Length + 3;

                string imageURIContent = FindPairComment(imageURLStartIndex, vendorsText, false);

                if (!string.IsNullOrEmpty(imageURIContent))
                {
                    string[] slashSplit = imageURIContent.Split('/');

                    if(slashSplit.Length > 1)
                    {
                        string imageName = slashSplit[1].Split('.')[0];

                        IMG_NAME_TO_URL.Add(imageName, imageURIContent);
                    }
                }
            }

            // END Analysis Resource Path

            // Second, Analysis SRC Path
            string srcObjectValue = "src\\:Object.values\\(Object.assign\\(";

            int srcIndex = 0;

            foreach (Match srcMatch in Regex.Matches(vendorsText, srcObjectValue))
            {
                int bracketBefore = srcMatch.Index - 1;

                string srcContent = FindPairBracket(bracketBefore, vendorsText, true);

                int startOffsetIndex = srcMatch.Index + 31;

                int srcURIIndex = startOffsetIndex + 2;

                string srcURIContent = FindPairComment(srcURIIndex, vendorsText, false);

                //UnityEngine.Debug.Log(srcURIContent);

                int idStartIndex = srcContent.IndexOf("id:") + 3;

                string idContent = FindPairComment(bracketBefore + idStartIndex, vendorsText, false);
                
                int srcURIEndIndex = srcURIIndex + srcURIContent.Length + 1;//GetStringIndexForward(vendorsText, srcURIIndex + srcURIContent.Length + 3, new char[] { '"' });

                int srcURITypeDetectIndex = srcURIEndIndex + 2;

                // Detect Type is Base 64
                if(vendorsText[srcURITypeDetectIndex] == '"')
                {
                    string base64ContentBetween = FindPairComment(srcURITypeDetectIndex, vendorsText, false);

                    string trimPattern = "data:image/png;base64,";

                    int trimIndex = base64ContentBetween.IndexOf(trimPattern);

                    if (trimIndex >= 0)
                    {
                        trimIndex += trimPattern.Length;

                        base64ContentBetween = base64ContentBetween.Substring(trimIndex);
                    }

                    NetSpineData data = new NetSpineData();

                    data.imageType = ParameterType.Base64;

                    data.imgUrl = base64ContentBetween;

                    data.manifestID = srcIndex;

                    data.manifestName = idContent;

                    MANIFEST_ID_TO_NAME.Add(data.manifestID, data);

                    if (!NAME_TO_MANIFESTLIST.ContainsKey(data.manifestName))
                    {
                        NAME_TO_MANIFESTLIST.Add(data.manifestName, new List<NetSpineData> { data });
                    }
                    else
                    {
                        NAME_TO_MANIFESTLIST[data.manifestName].Add(data);
                    }
                    
                    srcIndex++;
                }
                else
                {
                    NetSpineData data = new NetSpineData();

                    data.imageType = ParameterType.URL;

                    if (IMG_NAME_TO_URL.ContainsKey(idContent))
                    {
                        data.imgUrl = IMG_NAME_TO_URL[idContent];
                    }

                    data.manifestID = srcIndex;

                    data.manifestName = idContent;

                    MANIFEST_ID_TO_NAME.Add(data.manifestID, data);

                    if (!NAME_TO_MANIFESTLIST.ContainsKey(data.manifestName))
                    {
                        NAME_TO_MANIFESTLIST.Add(data.manifestName, new List<NetSpineData> { data });
                    }
                    else
                    {
                        NAME_TO_MANIFESTLIST[data.manifestName].Add(data);
                    }

                    srcIndex++;
                }
            }
            // End Analysis SRC Path

            // Third, Analysis ATLAS AND JSON Path

            string atlasPattern = "atlas:Object.values\\(Object.assign\\(";

            foreach (Match atlasMatch in Regex.Matches(vendorsText, atlasPattern))
            {
                int bracketStart = atlasMatch.Index - 1;

                string atlasAndJSONContent = FindPairBracket(bracketStart, vendorsText, true);

                int idStart = bracketStart - 2;

                string idContent = GetStringBackward(vendorsText, idStart, new char[] { ',', '{' });

                string atlasStartPattern = "atlas:Object.values(Object.assign({";

                int atlasStartIndex = atlasAndJSONContent.IndexOf(atlasStartPattern);

                string atlasURIPath = FindPairComment(atlasStartIndex + atlasStartPattern.Length, atlasAndJSONContent, true);

                string atlas = FindPairComment(atlasStartIndex + atlasStartPattern.Length + atlasURIPath.Length + 1, atlasAndJSONContent, false);

                atlas = atlas.Replace("\\n", "\n");

                atlas = atlas.Replace("\\t", "");

                if (NAME_TO_MANIFESTLIST.ContainsKey(idContent))
                {
                    foreach (var data in NAME_TO_MANIFESTLIST[idContent])
                    {
                        data.atlasString = atlas;

                        break;
                    }
                }
                else
                {
                    NetSpineData data = new NetSpineData();

                    data.atlasString = atlas;

                    NAME_TO_MANIFESTLIST.Add(idContent, new List<NetSpineData>() { data });

                    UnityEngine.Debug.Log("MISSING : Atlas " + idContent + "  has no relate resources.");
                }

                string jsonStartPattern = "json:Object.values(Object.assign({";

                int jsonStartIndex = atlasAndJSONContent.IndexOf(jsonStartPattern);

                string jsonURIPath = FindPairComment(jsonStartIndex + jsonStartPattern.Length, atlasAndJSONContent, true);

                string json = FindPairBracket(jsonStartIndex + jsonStartPattern.Length + jsonURIPath.Length + 1, atlasAndJSONContent, true);

                json = FormatJson(json);

                json = json.Replace("4.0-from-", "");

                json = json.Replace("4.1-from-", "");

                if (NAME_TO_MANIFESTLIST.ContainsKey(idContent))
                {
                    foreach (var data in NAME_TO_MANIFESTLIST[idContent])
                    {
                        data.jsonString = json;

                        break;
                    }
                }
                else
                {
                    NetSpineData data = new NetSpineData();

                    data.jsonString = json;

                    NAME_TO_MANIFESTLIST.Add(idContent, new List<NetSpineData>() { data });

                    UnityEngine.Debug.Log("MISSING : JSON " + idContent + "  has no relate resources.");
                }
            }
            // End Analysis ATLAS AND JSON Path
        }
        #endregion
        //// -------------------------  END OBJECT ASSIGN WEBSITE  -------------------------------
        watch.Stop();

        UnityEngine.Debug.Log("Decode Duration is " + (watch.Elapsed));

        #region Downloadoad Resources
        Dictionary<string, string> nameToStoragePath = new Dictionary<string, string>();

        bool DownloadMode = true;

        // First Deal With Valid Content
        foreach (var pair in MANIFEST_ID_TO_NAME)
        {
            string name = pair.Value.manifestName;

            bool hasImage = pair.Value.imageType != ParameterType.NoImage;
            bool hasAtlas = !string.IsNullOrEmpty(pair.Value.atlasString);

            bool hasJson = !string.IsNullOrEmpty(pair.Value.jsonUrl) || !string.IsNullOrEmpty(pair.Value.jsonString);

            bool isValid = hasImage && hasAtlas && hasJson;

            if (isValid)
            {
                string savePath = Application.dataPath + "/" + folderSubPath + "/" + name + "/";

                nameToStoragePath.Add(name, savePath);

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                string atlasPath = savePath + name + ".atlas.txt";

                if (!File.Exists(atlasPath))
                {
                    File.WriteAllText(atlasPath, pair.Value.atlasString);
                }

                string jsonPath = savePath + name + ".json";

                bool hasUrl = !string.IsNullOrEmpty(pair.Value.jsonUrl);

                bool hasContent = !string.IsNullOrEmpty(pair.Value.jsonString);

                if (hasUrl)
                {
                    string finalJsonPath = savePath + name + ".json";

                    if (!File.Exists(finalJsonPath))
                    {
                        string url = urlParent + pair.Value.jsonUrl;

                        string jsonSaveFile = savePath + name + ".json";

                        if (DownloadMode)
                        {
                            using (WebClient client = new WebClient())
                            {
                                client.Proxy = null;

                                client.DownloadFile(new Uri(url), jsonSaveFile);

                                string jsonText = File.ReadAllText(jsonSaveFile, Encoding.UTF8);

                                jsonText = jsonText.Replace("4.0-from-", "");

                                jsonText = jsonText.Replace("4.1-from-", "");

                                File.WriteAllText(jsonSaveFile, jsonText);

                                //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                            }
                        }
                    }
                }

                if (hasContent)
                {
                    if (!File.Exists(jsonPath))
                    {
                        File.WriteAllText(jsonPath, pair.Value.jsonString);
                    }
                }


                if (pair.Value.imageType == ParameterType.URL)
                {
                    string finalImagePath = savePath + name + ".png";

                    if (!File.Exists(finalImagePath))
                    {
                        string url = urlParent + pair.Value.imgUrl;

                        if (DownloadMode)
                        {
                            using (WebClient client = new WebClient())
                            {
                                client.Proxy = null;

                                client.DownloadFile(new Uri(url), savePath + name + ".png");

                                //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                            }
                        }
                    }
                }

                if (pair.Value.imageType == ParameterType.Base64)
                {
                    string base64SavePath = savePath + name + ".png";

                    if (!File.Exists(base64SavePath))
                    {
                        string base64String = pair.Value.imgUrl;

                        byte[] data = Convert.FromBase64String(base64String);

                        if (DownloadMode)
                        {
                            System.IO.File.WriteAllBytes(base64SavePath, data);
                        }
                    }
                }
            }
        }

        // Second Deal With Failed Content
        foreach (var pair in MANIFEST_ID_TO_NAME)
        {
            string name = pair.Value.manifestName;

            bool hasImage = pair.Value.imageType != ParameterType.NoImage;
            bool hasAtlas = !string.IsNullOrEmpty(pair.Value.atlasString);

            bool hasJson = !string.IsNullOrEmpty(pair.Value.jsonUrl) || !string.IsNullOrEmpty(pair.Value.jsonString);

            bool isValid = hasImage && hasAtlas && hasJson;

            if (!isValid)
            {
                string[] subNameList = name.Split('_');

                bool isFailedManaged = false;

                if (subNameList.Length > 1)
                {
                    List<string> subNameArray = new List<string>();
                    for (int i = 0; i < subNameList.Length; i++)
                    {
                        string subName = "";

                        for (int j = 0; j < i; j++)
                        {
                            subName += subNameList[j];

                            if ((j + 1) != i)
                            {
                                subName += "_";
                            }
                        }

                        if (!string.IsNullOrEmpty(subName))
                        {
                            subNameArray.Add(subName);
                        }
                    }

                    for (int i = subNameArray.Count - 1; i >= 0; i--)
                    {
                        string newDummyName = subNameArray[i];

                        if (nameToStoragePath.ContainsKey(newDummyName))
                        {
                            string bestMatchPath = nameToStoragePath[newDummyName];

                            if (!string.IsNullOrEmpty(pair.Value.atlasString))
                            {
                                string atlasPath = bestMatchPath + name + ".atlas.txt";

                                if (!File.Exists(atlasPath))
                                {
                                    File.WriteAllText(atlasPath, pair.Value.atlasString);
                                }
                            }

                            string jsonPath = bestMatchPath + name + ".json";

                            bool hasUrl = !string.IsNullOrEmpty(pair.Value.jsonUrl);

                            bool hasContent = !string.IsNullOrEmpty(pair.Value.jsonString);

                            if (hasUrl)
                            {
                                string finalJsonPath = bestMatchPath + name + ".json";

                                if (!File.Exists(finalJsonPath))
                                {
                                    string url = urlParent + pair.Value.jsonUrl;

                                    string jsonSaveFile = bestMatchPath + name + ".json";

                                    if (DownloadMode)
                                    {
                                        using (WebClient client = new WebClient())
                                        {
                                            client.Proxy = null;

                                            client.DownloadFile(new Uri(url), jsonSaveFile);

                                            string jsonText = File.ReadAllText(jsonSaveFile, Encoding.UTF8);

                                            jsonText = jsonText.Replace("4.0-from-", "");

                                            jsonText = jsonText.Replace("4.1-from-", "");

                                            File.WriteAllText(jsonSaveFile, jsonText);

                                            //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                                        }
                                    }
                                }
                            }

                            if (hasContent)
                            {
                                if (!File.Exists(jsonPath))
                                {
                                    File.WriteAllText(jsonPath, pair.Value.jsonString);
                                }
                            }

                            if (pair.Value.imageType == ParameterType.URL)
                            {
                                string finalImagePath = bestMatchPath + name + ".png";

                                if (!File.Exists(finalImagePath))
                                {
                                    string url = urlParent + pair.Value.imgUrl;

                                    if (DownloadMode)
                                    {
                                        using (WebClient client = new WebClient())
                                        {
                                            client.Proxy = null;

                                            client.DownloadFile(new Uri(url), bestMatchPath + name + ".png");

                                            //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                                        }
                                    }
                                }
                            }

                            if (pair.Value.imageType == ParameterType.Base64)
                            {
                                string base64SavePath = bestMatchPath + name + ".png";

                                if (!File.Exists(base64SavePath))
                                {
                                    string base64String = pair.Value.imgUrl;

                                    byte[] data = Convert.FromBase64String(base64String);

                                    if (DownloadMode)
                                    {
                                        System.IO.File.WriteAllBytes(base64SavePath, data);
                                    }
                                }
                            }

                            isFailedManaged = true;

                            break;
                        }
                    }
                }

                // No match at all. Save Them In Other Folder
                if (!isFailedManaged)
                {
                    string FailedPath = Application.dataPath + "/" + folderSubPath + "/" + "otherResources" + "/";

                    if (!Directory.Exists(FailedPath))
                    {
                        Directory.CreateDirectory(FailedPath);
                    }

                    if (!string.IsNullOrEmpty(pair.Value.atlasString))
                    {
                        string atlasPath = FailedPath + name + ".atlas.txt";

                        if (!File.Exists(atlasPath))
                        {
                            File.WriteAllText(atlasPath, pair.Value.atlasString);
                        }
                    }

                    string jsonPath = FailedPath + name + ".json";

                    bool hasUrl = !string.IsNullOrEmpty(pair.Value.jsonUrl);

                    bool hasContent = !string.IsNullOrEmpty(pair.Value.jsonString);

                    if (hasUrl)
                    {
                        string finalJsonPath = FailedPath + name + ".json";

                        if (!File.Exists(finalJsonPath))
                        {
                            string url = urlParent + pair.Value.jsonUrl;

                            string jsonSaveFile = FailedPath + name + ".json";

                            if (DownloadMode)
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.Proxy = null;

                                    client.DownloadFile(new Uri(url), jsonSaveFile);

                                    string jsonText = File.ReadAllText(jsonSaveFile, Encoding.UTF8);

                                    jsonText = jsonText.Replace("4.0-from-", "");

                                    jsonText = jsonText.Replace("4.1-from-", "");

                                    File.WriteAllText(jsonSaveFile, jsonText);

                                    //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                                }
                            }
                        }
                    }

                    if (hasContent)
                    {
                        if (!File.Exists(jsonPath))
                        {
                            File.WriteAllText(jsonPath, pair.Value.jsonString);
                        }
                    }

                    if (pair.Value.imageType == ParameterType.URL)
                    {
                        string finalImagePath = FailedPath + name + ".png";

                        if (!File.Exists(finalImagePath))
                        {
                            string url = urlParent + pair.Value.imgUrl;

                            if (DownloadMode)
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.Proxy = null;

                                    client.DownloadFile(new Uri(url), FailedPath + name + ".png");

                                    //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                                }
                            }
                        }
                    }

                    if (pair.Value.imageType == ParameterType.Base64)
                    {
                        string base64SavePath = FailedPath + name + ".png";

                        if (!File.Exists(base64SavePath))
                        {
                            string base64String = pair.Value.imgUrl;

                            byte[] data = Convert.FromBase64String(base64String);

                            if (DownloadMode)
                            {
                                System.IO.File.WriteAllBytes(base64SavePath, data);
                            }
                        }
                    }
                }

            }

        }
        #endregion

        #region Download Geometry

        string resPath = Application.dataPath + "/" + folderSubPath + "/" + "otherResources/";

        if (!Directory.Exists(resPath))
        {
            Directory.CreateDirectory(resPath);
        }

        string geometrySaveFolder = Application.dataPath + "/" + folderSubPath + "/";

        if (!Directory.Exists(geometrySaveFolder))
        {
            Directory.CreateDirectory(geometrySaveFolder);
        }

        string geometryPattern = firstFuncName + ".exports=JSON.parse\\(\'";

        int geoIndex = 0;

        int otherJsonIndex = 0;

        bool findGeometry = false;

        foreach (Match geoMatch in Regex.Matches(vendorsText, geometryPattern))
        {
            int startIndex = geoMatch.Index + 21;

            string jsonContent = FindPairComment(startIndex, vendorsText, false);

            if (jsonContent.Contains("\"geometries\""))
            {
                string suffix = geoIndex == 0 ? "" : ("" + geoIndex);

                string geoSavePath = geometrySaveFolder + folderSubPath + "_" + suffix + "Geo.json";

                if (!File.Exists(geoSavePath))
                {
                    findGeometry = true;
                    File.WriteAllText(geoSavePath, jsonContent);
                }

                geoIndex++;
            }
            else if(!jsonContent.StartsWith("{\"skeleton\":{\""))
            {
                //string resPath = Application.dataPath + "/" + folderSubPath + "/" + "otherResources/";

                string suffix = "" + otherJsonIndex;

                string otherJsonSavePath = resPath + "json_" + suffix + "_Other.json";

                if (!File.Exists(otherJsonSavePath))
                {
                    File.WriteAllText(otherJsonSavePath, jsonContent);
                }

                otherJsonIndex++;
            }
        }

        if (!findGeometry)
        {
            string anotherGeoPattern = "geometries:\\{";

            foreach (Match geoMatch in Regex.Matches(vendorsText, anotherGeoPattern))
            {
                int jsonStartIndex = geoMatch.Index + 11;

                string jsonContent = FindPairBracket(jsonStartIndex, vendorsText, false);

                if (!string.IsNullOrEmpty(jsonContent))
                {
                    int fullJsonStartIndex = geoMatch.Index - 1;

                    string fullJson = FindPairBracket(fullJsonStartIndex, vendorsText, true);

                    string suffix = geoIndex == 0 ? "" : ("" + geoIndex);

                    string geoSavePath = geometrySaveFolder + folderSubPath + "_" + suffix + "Geo.json";

                    if (!File.Exists(geoSavePath))
                    {
                        findGeometry = true;

                        StringBuilder jsonContentBuilder = new StringBuilder();

                        jsonContentBuilder.Append(fullJson);

                        string json = jsonContentBuilder.ToString();

                        json = FormatJson(json);

                        File.WriteAllText(geoSavePath, json);
                    }

                    geoIndex++;
                }
                
            }
        }
        #endregion

        #region Download Not Spine Resources

        foreach (var pair in INDEX_ID_TO_RESOURCE)
        {
            if (!ALREADY_USE_INDEX_RESOURCES.ContainsKey(pair.Key))
            {
                //string resPath = Application.dataPath + "/" + folderSubPath + "/" + "otherResources/";

                string resBetterName = "";

                if (pair.Value.StartsWith("data:image"))
                {
                    if (DownloadMode)
                    {
                        string pairKeyName = pair.Key;

                        if (pairKeyName.Contains("\\"))
                        {
                            pairKeyName = pairKeyName.Replace('\\', '_');
                        }

                        if (pairKeyName.Contains("/"))
                        {
                            pairKeyName = pairKeyName.Replace('/', '_');
                        }

                        resBetterName = pairKeyName + ".png";

                        string storagePath = resPath + resBetterName;

                        int firstDotIndex = pair.Value.IndexOf(',');

                        string base64Content = pair.Value.Substring(firstDotIndex + 1);

                        byte[] data = Convert.FromBase64String(base64Content);

                        System.IO.File.WriteAllBytes(storagePath, data);
                    }

                    continue;
                }
                else if (pair.Value.EndsWith(".json") || pair.Value.EndsWith(".png") || pair.Value.EndsWith(".jpg") || pair.Value.EndsWith(".jpeg")
                    || pair.Value.EndsWith(".bin") || pair.Value.EndsWith(".mp3") || pair.Value.EndsWith(".gif"))
                {
                    string[] resNameList = pair.Value.Split('/');

                    string resName = resNameList[resNameList.Length - 1];

                    string[] resDotName = resName.Split('.');

                    resBetterName = resDotName[0] + "." + resDotName[resDotName.Length - 1];

                    if (!Directory.Exists(resPath))
                    {
                        Directory.CreateDirectory(resPath);
                    }

                    string storagePath = resPath + resBetterName;

                    if (!File.Exists(storagePath))
                    {
                        if (DownloadMode)
                        {
                            string url = urlParent + pair.Value;

                            //UnityEngine.Debug.Log(url);

                            using (WebClient client = new WebClient())
                            {
                                client.Proxy = null;

                                client.DownloadFile(new Uri(url), storagePath);

                                //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                            }
                        }
                    }
                }
            }
        }

        foreach (var pair in VENDOR_ID_TO_RESOURCE)
        {
            if (!ALREADY_USE_VENDORS_RESOURCES.ContainsKey(pair.Key))
            {
                //string resPath = Application.dataPath + "/" + folderSubPath + "/" + "otherResources/";

                string resBetterName = "";

                if (pair.Value.StartsWith("data:image"))
                {
                    if (DownloadMode)
                    {
                        resBetterName = pair.Key + ".png";

                        string storagePath = resPath + resBetterName;

                        int firstDotIndex = pair.Value.IndexOf(',');

                        string base64Content = pair.Value.Substring(firstDotIndex + 1);

                        byte[] data = Convert.FromBase64String(base64Content);

                        System.IO.File.WriteAllBytes(storagePath, data);
                    }

                    continue;
                }
                else if (pair.Value.EndsWith(".json") || pair.Value.EndsWith(".png") || pair.Value.EndsWith(".jpg") || pair.Value.EndsWith(".jpeg")
                    || pair.Value.EndsWith(".bin") || pair.Value.EndsWith(".mp3") || pair.Value.EndsWith(".gif"))
                {
                    string[] resNameList = pair.Value.Split('/');

                    string resName = resNameList[resNameList.Length - 1];

                    string[] resDotName = resName.Split('.');

                    resBetterName = resDotName[0] + "." + resDotName[resDotName.Length - 1];

                    if (!Directory.Exists(resPath))
                    {
                        Directory.CreateDirectory(resPath);
                    }

                    string storagePath = resPath + resBetterName;

                    if (!File.Exists(storagePath))
                    {
                        if (DownloadMode)
                        {
                            string url = urlParent + pair.Value;

                            //UnityEngine.Debug.Log(url);

                            using (WebClient client = new WebClient())
                            {
                                client.Proxy = null;

                                client.DownloadFile(new Uri(url), storagePath);

                                //Thread.Sleep(UnityEngine.Random.Range(8, 15));
                            }
                        }
                    }
                }
            }
        }
        #endregion
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log("Finish Reading Vendors! Enjoy!");

        return;
    }

    static string FindJsonPairComment(int startIndex, string origin, bool withBracket)
    {
        StringBuilder stringBuilder = new StringBuilder();

        char startBracket = origin[startIndex];

        int before = startIndex - 1;

        if (startBracket != '\"' && startBracket != '\'')
            return stringBuilder.ToString();

        char beforeChar = '@';

        if (before >= 0)
        {
            beforeChar = origin[before];
        }

        char afterChar = '@';

        int after = startIndex + 1;

        for (int i = startIndex; i < origin.Length; i++)
        {
            char current = origin[i];

            stringBuilder.Append(current);

            if (current == startBracket && i != startIndex)
            {
                switch (beforeChar)
                {
                    case '(':
                        afterChar = '@';

                        after = i + 1;

                        if (after < origin.Length)
                        {
                            afterChar = origin[after];
                        }

                        if (afterChar != ')')
                            continue;
                        break;
                    case '[':
                        afterChar = '@';
                        if (after < origin.Length)
                        {
                            afterChar = origin[after];
                        }

                        if (afterChar != ']')
                            continue;
                        break;
                    case '{':
                        afterChar = '@';
                        if (after < origin.Length)
                        {
                            afterChar = origin[after];
                        }

                        if (afterChar != '}')
                            continue;
                        break;
                }
                break;
            }

        }
        string result = stringBuilder.ToString();

        if (withBracket)
        {
            return result;
        }
        else
        {
            if (result.Length > 2) // May be trucate by origin.Length
            {
                return result.Substring(1, result.Length - 2);
            }
        }
        //result = withBracket ? result : result.Substring(1, result.Length - 2);
        return result;
    }

    static string FindPairComment(int startIndex, string origin, bool withBracket)
    {
        StringBuilder stringBuilder = new StringBuilder();

        char startBracket = origin[startIndex];

        if (startBracket != '\"' && startBracket != '\'')
            return stringBuilder.ToString();

        for (int i = startIndex; i < origin.Length; i++)
        {
            char current = origin[i];

            stringBuilder.Append(current);

            if (current == startBracket && i != startIndex)
            {
                break;
            }

        }

        string result = stringBuilder.ToString();

        if (withBracket)
        {
            return result;
        }
        else
        {
            if(result.Length > 2) // May be trucate by origin.Length
            {
                return result.Substring(1, result.Length - 2);
            }
        }
        //result = withBracket ? result : result.Substring(1, result.Length - 2);
        return result;
    }

    static string FindPairBracket(int startIndex, string origin, bool withBracket)
    {
        StringBuilder stringBuilder = new StringBuilder();

        char startBracket = origin[startIndex];

        char endBracket = ':';

        switch (startBracket)
        {
            case '(':
                endBracket = ')';
                break;

            case '[':
                endBracket = ']';
                break;

            case '{':
                endBracket = '}';
                break;
        }

        int startBracketCount = 0;


        for (int i = startIndex; i < origin.Length; i++)
        {
            char current = origin[i];

            if (current == startBracket)
            {
                startBracketCount++;
            }
            else if (current == endBracket)
            {
                startBracketCount--;
            }

            stringBuilder.Append(current);

            if (startBracketCount == 0)
            {
                break;
            }

        }

        string result = stringBuilder.ToString();

        result = withBracket ? result : result.Substring(1, result.Length - 2);
        return result;
    }

    static (bool, int) ConvertJSNumber(string num)
    {
        bool success = true;

        int number = 0;

        string s_Num = num.ToLower();

        try
        {
            if (s_Num.Contains("e"))
            {
                string[] s_IDSep = s_Num.Split('e');

                number = int.Parse(s_IDSep[0]) * (int)Mathf.Pow(10, int.Parse(s_IDSep[1]));
            }
            else
            {
                number = int.Parse(s_Num);
            }
        }
        catch (Exception e)
        {
            return (false, -1);
        }

        return (success, number);
    }

    static bool DecideBase64Case(List<string> PARAMETER_ARRAY, Dictionary<int, NetSpineData> MANIFEST_DICT, int PARAMETER_ID, int MANIFEST_ID, string text, Dictionary<string, string> id_in_index, Dictionary<string, string> id_in_vendors)
    {
        if (MANIFEST_DICT.ContainsKey(MANIFEST_ID))
        {
            if (MANIFEST_DICT[MANIFEST_ID].imageType == ParameterType.Base64 || MANIFEST_DICT[MANIFEST_ID].imageType == ParameterType.URL)
                return false;
        }

        string sPrefix = PARAMETER_ARRAY[PARAMETER_ID];

        string contentBetween = "";

        if (string.IsNullOrEmpty(contentBetween))
        {
            if (id_in_index.ContainsKey(sPrefix))
            {
                string toCheck = id_in_index[sPrefix];

                if (toCheck.StartsWith("data:image/png;base64,"))
                    contentBetween = id_in_index[sPrefix];
            }
        }

        if (string.IsNullOrEmpty(contentBetween))
        {
            if (id_in_vendors.ContainsKey(sPrefix))
            {
                string toCheck = id_in_vendors[sPrefix];

                if (toCheck.StartsWith("data:image/png;base64,"))
                    contentBetween = id_in_vendors[sPrefix];
            }
        }

        if (string.IsNullOrEmpty(contentBetween))
        {
            string prefix = "" + PARAMETER_ARRAY[PARAMETER_ID];

            if (prefix.Contains("+"))
            {
                prefix = prefix.Replace("+", "\\+");
            }

            string base64Pattern = prefix + ":function\\(e\\){\"use strict\";e.exports=";

            foreach (Match perLineMatch in Regex.Matches(text, base64Pattern))
            {
                int findColonIndex = base64Pattern.IndexOf(':') - 1;

                string doubleCheckPrefix = GetStringBackward(text, perLineMatch.Index + findColonIndex, new char[] { ',', '{' });

                if (doubleCheckPrefix.Equals(prefix))
                {
                    int bracketStartIndex = perLineMatch.Index + perLineMatch.Value.Length;

                    contentBetween = FindPairComment(bracketStartIndex, text, false);

                    break;
                }
            }
        }

        bool isChecked = false;

        if (!string.IsNullOrEmpty(contentBetween))
        {
            string trimPattern = "data:image/png;base64,";

            int trimIndex = contentBetween.IndexOf(trimPattern);

            if (trimIndex >= 0)
            {
                trimIndex += trimPattern.Length;

                contentBetween = contentBetween.Substring(trimIndex);

                MANIFEST_DICT[MANIFEST_ID].imgUrl = contentBetween;

                MANIFEST_DICT[MANIFEST_ID].imageType = ParameterType.Base64;

                MANIFEST_DICT[MANIFEST_ID].relateAllParameterContent = PARAMETER_ARRAY[PARAMETER_ID];

                MANIFEST_DICT[MANIFEST_ID].relateAllParameterIndex = PARAMETER_ID;

                isChecked = true;
            }
        }

        return isChecked;
    }

    static string GetStringBackward(string origin, int startIndex, char[] stopWords)
    {
        StringBuilder resultBuilder = new StringBuilder();

        char current = origin[startIndex];

        while (!CharInArray(current, stopWords))
        {
            resultBuilder.Append(current);

            startIndex -= 1;

            if (startIndex < 0)
            {
                break;
            }
            current = origin[startIndex];
        }
        string temp = resultBuilder.ToString();

        string result = StringReverse(temp);
        return result;
    }

    static int GetStringIndexBackward(string origin, int startIndex, char[] stopWords)
    {
        char current = origin[startIndex];

        while (!CharInArray(current, stopWords))
        {
            startIndex -= 1;

            if (startIndex < 0)
            {
                startIndex = 0;
                break;
            }
            current = origin[startIndex];
        }
        return startIndex;
    }

    static int GetStringIndexForward(string origin, int startIndex, char[] stopWords)
    {
        char current = origin[startIndex];

        while (!CharInArray(current, stopWords))
        {
            startIndex += 1;

            if (startIndex > (origin.Length - 1))
            {
                startIndex = (origin.Length - 1);
                break;
            }
            current = origin[startIndex];
        }
        return startIndex;
    }

    static bool CharInArray(char c, char[] charArray)
    {
        foreach (var ch in charArray)
        {
            if (c == ch)
                return true;
        }

        return false;
    }

    public static string StringReverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    public static int GetNumberByTag(string origin, string tagName)
    {
        string pattern = "(?:" + tagName + "\\:n)\\([0-9]*\\)";

        foreach (Match match in Regex.Matches(origin, pattern))
        {
            string getContentInBracket = "(?<=\\()(.+?)(?=\\))";

            foreach (Match match1 in Regex.Matches(match.Value, getContentInBracket))
            {
                var pair = ConvertJSNumber(match1.Value);

                return pair.Item1 ? pair.Item2 : -1;
            }
        }

        return -1;
    }

    public static bool IsResources(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        bool isBase64 = content.StartsWith("data:image");

        if (isBase64)
            return true;

        if (content.Contains("{") || content.Contains(";"))
            return false;

        int lastDotIndex = content.LastIndexOf('.');

        bool isResources = lastDotIndex > 0 && (content.Length - lastDotIndex) < 7;

        return isResources;
    }

    public static string TrimQuote(string content, char target)
    {
        StringBuilder builder = new StringBuilder();

        string[] contentList = content.Split(target);

        for (int i = 0; i < contentList.Length; i++)
        {
            builder.Append(contentList[i]);
        }

        return builder.ToString();
    }

    public static string ConvertToValidPrefix(string content)
    {
        if (content.Contains("+"))
        {
            content = AdvancedInsert(content, "+", "\\");
        }

        if (content.Contains(")"))
        {
            content = AdvancedInsert(content, ")", "\\");
        }

        if (content.Contains("("))
        {
            content = AdvancedInsert(content, "(", "\\");
        }

        return content;
    }

    public static bool IsValidPrefix(string content)
    {
        if (content.Contains(":"))
            return false;

        if (content.Contains("{"))
            return false;

        if (content.Contains("}"))
            return false;

        if (content.Contains("["))
            return false;

        if (content.Contains("]"))
            return false;

        if (content.Contains("."))
            return false;

        if (content.Contains(";"))
            return false;

        return true;
    }

    public static string AdvancedInsert(string content, string pattern, string insert, int count = 1)
    {
        int head = 0;
        int end = content.IndexOf(pattern);

        StringBuilder res = new StringBuilder();

        while (end >= 0)
        {
            res.Append(content.Substring(head, end - head));

            for (int i = 0; i < count; i++)
            {
                res.Append(insert);
            }

            head = end;

            end = content.IndexOf(pattern, end + 1);
        }

        res.Append(content.Substring(head));

        return res.ToString();

    }

    public static string GetFileName(string origin)
    {
        StringBuilder stringBuilder = new StringBuilder();

        string[] splitOrigin = origin.Split('.');

        string nameParam = splitOrigin[0];

        int startIndex = nameParam.LastIndexOf('/');

        if (startIndex >= 0)
            nameParam = nameParam.Substring(startIndex + 1);

        return nameParam;
    }

    public static string GetContentByTag(string origin, string tagName)
    {
        string sub1 = origin.Substring(tagName.Length + 1);

        string sub2 = sub1.Substring(0, sub1.Length - 1);

        if (sub2.StartsWith("\""))
        {
            sub2 = FindPairComment(0, sub2, false);
        }

        return sub2;
    }

    public static string FindMostPopString(Dictionary<string, int> content)
    {
        int maxFuncNameCount = -1;

        string result = "";

        foreach (var pair in content)
        {
            if (pair.Value > maxFuncNameCount)
            {
                maxFuncNameCount = pair.Value;

                result = pair.Key;
            }
        }

        return result;
    }
}
