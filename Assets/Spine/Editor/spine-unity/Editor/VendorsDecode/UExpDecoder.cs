using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class UExpDecoder : AssetPostprocessor
{
    void OnPreprocessAsset()
    {
        if (!assetPath.EndsWith(".uexp"))
            return;

        int firstIndex = assetPath.IndexOf('/');

        int lastIndex = assetPath.LastIndexOf('/');

        string assetRelativeFolderPath = assetPath.Substring(0, lastIndex);

        string assetName = assetPath.Substring(lastIndex + 1);

        int pointIndex = assetName.LastIndexOf('.');

        string assetSubName = assetName.Substring(0, pointIndex);

        string atlasPath = assetRelativeFolderPath + "/" + assetSubName + ".atlas.txt";

        if (File.Exists(atlasPath))
        {
            return;
        }

        string assetRelativePath = assetPath.Substring(firstIndex);

        string assetAbsPath = Application.dataPath + assetRelativePath;

        if (File.Exists(assetAbsPath))
        {
            string configContent = File.ReadAllText(assetAbsPath, Encoding.UTF8);

            int atlasStartIndex = configContent.IndexOf(assetSubName);

            string atlasContent = "\n" + ReadStringUntilZeroFramUExp(configContent, atlasStartIndex);

            if (!string.IsNullOrEmpty(atlasContent))
            {
                if (!File.Exists(atlasPath))
                {
                    File.WriteAllText(atlasPath, atlasContent);
                }
            }

            int skeletonIndex = configContent.IndexOf("skeleton");

            int startIndex = skeletonIndex;

            while(configContent[startIndex] != '{')
            {
                startIndex--;
            }

            string skeletonJson = ReadStringUntilZeroFramUExp(configContent, startIndex);

            int lastBracket = skeletonJson.LastIndexOf('}');

            skeletonJson = skeletonJson.Substring(0, lastBracket + 1);

            string skeletonPath = assetRelativeFolderPath + "/" + assetSubName + ".json";

            if (!string.IsNullOrEmpty(skeletonJson))
            {
                if (!File.Exists(skeletonPath))
                {
                    File.WriteAllText(skeletonPath, skeletonJson);
                }
            }
        }

        AssetDatabase.Refresh();
    }

    static string ReadStringUntilZeroFramUExp(string origin, int startIndex)
    {
        StringBuilder sb = new StringBuilder();

        int start = startIndex;

        char stopWord = '\0';

        while(origin[start] != stopWord)
        {
            sb.Append(origin[start]);

            start++;
        }

        return sb.ToString();
    }
}
