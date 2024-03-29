# WaifuSpineRuntime

The **spine-unity** runtime provides functionality to load, manipulate and render [Spine](http://esotericsoftware.com) skeletal animation data using [Unity](http://unity3d.com/). 

But I added some other feature in it.

Including:

1. Support Spine 3.5 to Spine 4.2 import without modification.(binary only, json still in progress).

2. Extract mihoyo / hoyoverse website and decode Spine correctly.

3. Add mihoyo / hoyoverse website spine runtime movement and hierachy rebuild.

# Preparation

1. The Repo is Already a workable project for Unity2020 and above. Download it and Open it with Unity, you are good to go!

   (branch for Unity 2017 is also provided, but it is always recomand to use Unity in newer version.)

2. To Avoid Texture alpha looks weird, go to **Edit**->**Preference**->**Spine** in menu and set the Spine Editor Settings to the picture bellow.
   (Just Use **Straight Alpha Preset**)

![Transparent_Settings](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Transparent_Settings.jpg)


    Tips:
    
      if you have no idea why the second step makes alpha looks good.
      
      read the [Spine-Unity-Document](https://esotericsoftware.com/spine-unity) especially **Alpha Texture Settings** section.
# Usage

## multiple spine version support
1. For this feature, just drag the relate **Atlas** **Texture** and **Skeleton** file into Unity. It should decode them correctly.

  For example:
* Arknights-> **Spine 3.5** (in latest, it has changed to 3.8)
* Princess Connect! Re:Dive -> **Spine 3.6**
* Azurlane -> **Spine 3.8**
* Blue Archive -> **Spine 3.8**
* Genshin Impact -> **Spine 4.0+**

![Feature_1_Example](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Feature_1_Example.jpg)

## Extract Mihoyo / Hoyoverse Website Spine
1. For this feature,  Go to **ZeroFly** in menu. Click the **Read Vendors Content**, it should open up a new window, drag it to wherever you want.

![Read_Content](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content.jpg)

2. Enter the website url, make sure it is accesable through browser.

![Read_Content_UI](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content_UI.jpg)

    **Tips:** You can use this link as an example :https://act.mihoyo.com/ys/event/e20231209preview-yh731z/index.html

3. Check the info on board.
   It should show following content:

![Read_Content_INFO](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content_UI_INFO.jpg)

* Zone Url
* Event Name
* Website Url

4. If every thing looks good, click the **Start Decode** button.

5. Wait until the program end.(It might take a while if the Spine import is really slow. I will try to improve it in future)

6. If no error occur. Check the **Assets** folder, a new folder wich named by **Event Name** should be generated.

![Read_Content_INFO](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content_Result.jpg)

7. The spine files are stored in the folder beneath.

## Implement Mihoyo / Hoyoverse Website Spine Movement

  #### Concept Explain: What is Mihoyo / Hoyoverse Website Spine Movement?
  
  In many cases, the hair or tails of characters are not hard coded in Spine file, the movements are generated by runtime algorithm. 
  
  That's why you can not view the tails movement in official Spine.

1. For this feature, drag the SkeletonDataAssets into the scene view.

![Movement_Drag](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Movement_Drag.jpg)

2. Add Component **Skeleton Utility**.

3. Click the **Spawn Hierachy** -> **Follow All Bones**

![Movement_Add](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Movement_Add.jpg)

4. Hit on the Unity Play Button, you should see the movement in runtime.

## Rebuild Mihoyo / Hoyoverse Website Hierachy

1. For this feature, create an empty GameObject in the hierrachy view by right click.

2. Add Component **Vendors Generator**

3. For every **Event Name** folder, which generated by Vendors Decoder, there should be one or more json file named with suffix **_Geo**

4. Drag it to **Geometry Json** in Inspector view.

![Generate_Geo](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Generate_Geo.jpg)

5. Click the **Spawn Geometry**

6. Check the generated gameobject hierachy, the first child node is always the root of scene node.

![Generate_Geo_Result](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Generate_Geo_Result.jpg)

7. Use set active/inactive to show only the scene you interest

8. Hit on the Unity Play Button,you can see spine animation, particles should work as expected.

**Experiment Feature:** 

1. For every **Event Name** folder, there are a child folder named **otherResources**, all the unknown usage files are downloaded there.
 
2. There might json files named like **json_0_Other**,**json_1_Other**...etc, they might be animation files, you can drag them to **Animation File List**,then Click the **Spawn Geometry**

3. You wil find animation files generated in **otherResources** folder.

4. Add Animation Component on the GameObject where **VendorsGenerator** is attached.
 
5. Hit on the Unity Play Button, the animation should work.


# Frequently Asked Question

#### 1. Why decode Mihoyo / Hoyoverse Website failed?

   A: The decode is base on vendors.js and index.js analization. Due to the inconsitent between website content. It might failed sometimes, especially when the website is before 2021/09/28. (The first aniversary review, I have tested it works.)
   
   Because the Mihoyo team change website js format in older version. For example, in older version, some website use bundle.js instead of index.js. The bundle.js format is really different from index.js

   To support older Website bring in too much pain, it would be better to start another project to do so.
   
   However, in newer version website, they finally come to a much stable format.
   
   If the decode failed, feel free to write issue, remember to attach the website url and what error Unity reports.
   
   The Website too old would not support though.

#### 2. Why My Spine Texture Transparency looks weired?

   Please look at the second point in Preparation, or look at the document [Spine-Unity-Document](https://esotericsoftware.com/spine-unity) especially **Alpha Texture Settings** section.
   
   Then remove the imported spine files and reimport them in Unity again.

#### 3. Why My Mihoyo Spine file looks weired in Official Spine Editor?
   
   (1) First, cloths, hair and tails might be generate by runtime scripts, not hard coded in spine file.
   
   (2) Second, the deform animation of attachments is bring out in json hierachy, so the official spine can not read deform animation correctly. In this runtime, it just skip the wrong hierachy to get correct result. In the future there will be scripts fix the case, so that the official spine can play mihoyo spine correctly.

# Last But Not Least

There are only scripts in repo. You should check the rights and avoid legal problems when using the resources get by this repo, especially usage in business!

If there are infringement in this repo. Just let me notice, I will delete relate resources as soon as possible.
   

## The Following Content is the Original unity-spine-runtime readme

# spine-unity

The **spine-unity** runtime provides functionality to load, manipulate and render [Spine](http://esotericsoftware.com) skeletal animation data using [Unity](http://unity3d.com/). spine-unity is based on [spine-csharp](../spine-csharp).

## Licensing

You are welcome to evaluate the Spine Runtimes and the examples we provide in this repository free of charge.

You can integrate the Spine Runtimes into your software free of charge, but users of your software must have their own [Spine license](https://esotericsoftware.com/spine-purchase). Please make your users aware of this requirement! This option is often chosen by those making development tools, such as an SDK, game toolkit, or software library.

In order to distribute your software containing the Spine Runtimes to others that don't have a Spine license, you need a [Spine license](https://esotericsoftware.com/spine-purchase) at the time of integration. Then you can distribute your software containing the Spine Runtimes however you like, provided others don't modify it or use it to create new software. If others want to do that, they'll need their own Spine license.

For the official legal terms governing the Spine Runtimes, please read the [Spine Runtimes License Agreement](http://esotericsoftware.com/spine-runtimes-license) and Section 2 of the [Spine Editor License Agreement](http://esotericsoftware.com/spine-editor-license#s2).

## Spine version

spine-unity works with data exported from Spine 4.1.xx.

spine-unity supports all Spine features.

Unity's physics components do not support dynamically assigned vertices so they cannot be used to mirror bone-weighted and deformed BoundingBoxAttachments. However, BoundingBoxAttachment vertices at runtime will still deform correctly and can be used to perform manual hit detection.

## Unity version

spine-unity is compatible with Unity 2017.1-2023.1.

## Usage

### [Please see the spine-unity guide for full documentation](http://esotericsoftware.com/spine-unity).

1. Create an empty Unity project (or use an existing project).
2. Download and import the [`spine-unity.unitypackage`](http://esotericsoftware.com/spine-unity-download/).

See the [Spine Runtimes documentation](http://esotericsoftware.com/spine-documentation#runtimesTitle) on how to use the APIs and check out the spine-unity examples for demonstrations of Unity specific features.

## Example

### [Please see the spine-unity guide for full documentation](http://esotericsoftware.com/spine-unity).

1. Create an empty Unity project
2. Download and import the [`spine-unity.unitypackage`](http://esotericsoftware.com/spine-unity-download/).
3. Explore the example scenes found in the `Assets/Spine Examples/Scenes` folder.

## Notes

- This slightly outdated [spine-unity tutorial video](http://www.youtube.com/watch?v=x1umSQulghA) may still be useful.
- Atlas images should use **Premultiplied Alpha** when using the shaders that come with spine-unity (`Spine/Skeleton` or `Spine/SkeletonLit`).
- Texture artifacts from compression: Unity's 2D project defaults import new images added to the project with the Texture Type "Sprite". This can cause artifacts when using the `Spine/Skeleton` shader. To avoid these artifacts, make sure the Texture Type is set to "Texture". spine-unity's automatic import will attempt to apply these settings but in the process of updating your textures, these settings may be reverted.
