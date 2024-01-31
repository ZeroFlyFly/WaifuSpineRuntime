# WaifuSpineRuntime

[For English User](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/README_en.md)

**spine-unity** 是一个Unity插件，目的是在[Unity](http://unity3d.com/)中加载并播放 [Spine](http://esotericsoftware.com) 骨骼动画。 

不过和官方的版本比起来，我添加了一些功能

包括:

1. 支持Spine 3.5到Spine 4.2版本的无痛导入(目前只支持二进制骨骼, json骨骼仍在施工中)。

2. 一键解析 mihoyo / hoyoverse 网页并能下载Spine文件并正确导入。

3. 添加实现 mihoyo / hoyoverse 网页spine动态效果和网页层级重建。

# 事前准备

1. 该仓库已经是一个可用的Unity项目，可以用Unity2020以上版本打开。只需要下载并用Unity打开，就可以了。

   (我也准备放出一个Unity 2017兼容版本，但是如果可以的话，还是建议升级Unity版本。）

2. 为了防止导入Unity中的贴图透明看起来不正确，请去到菜单栏**Edit**->**Preference**->**Spine** 按照下图所示，修改Spine Runtime的默认配置。
   (就用**Straight Alpha Preset**配置即可)

![Transparent_Settings](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Transparent_Settings.jpg)


    提示:
    
      如果你不清楚为什么要这么做
      
      可以阅读官方文档 [Spine-Unity-Document](https://esotericsoftware.com/spine-unity) 特别是 **Alpha Texture Settings** 部分.

# 用法

## 多版本Spine导入兼容
1. 对于这个功能, 只需要拖入对应的 **Atlas** **Texture** and **Skeleton** 文件进Unity。它就能正确解析。

  比如:
* 明日方舟-> **Spine 3.5** (最新版本, 已经是3.8版本)
* 公主连结 Re:Dive-> **Spine 3.6**
* 碧蓝航线 -> **Spine 3.8**
* 蔚蓝档案 -> **Spine 3.8**
* 原神(米家) -> **Spine 4.0+**

![Feature_1_Example](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Feature_1_Example.jpg)

## 一键提取 Mihoyo / Hoyoverse 网页Spine
1. 对于该功能,  菜单栏选择 **ZeroFly**。 点击 **Read Vendors Content**, 对应的解析窗体就会出现，你可以拖动到任意布局。

![Read_Content](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content.jpg)

2. 输入网页，当然要确认输入的网址确实是能访问到网页的。

![Read_Content_UI](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content_UI.jpg)

    **提示:** 你可以用这个链接作为例子 :https://act.mihoyo.com/ys/event/e20231209preview-yh731z/index.html

3. 检查下信息面板.
   它会提供如下内容:

![Read_Content_INFO](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content_UI_INFO.jpg)

* 域名
* 活动名
* 网页完整地址

4. 如果这些信息看着是正确的, 就可以点击**Start Decode**按钮了。

5. 等待一段时间直到进度条结束。(因为导入大量Spine会比较慢，所以要等一段时间，后续会想办法优化这部分)

6. 如果程序能顺利结束。 看看工程里的 **Assets** 文件夹, 会有一个新文件夹生成，名字就是用的 **活动名**。

![Read_Content_INFO](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Read_Content_Result.jpg)

7. Spine 文件就保存在子文件夹中。

## 实现 Mihoyo / Hoyoverse 网页Spine动态效果

  #### 概念解析: 什么是 Mihoyo / Hoyoverse 网页动态效果?
  
  在很多情况下, 角色的头发衣服尾巴，都不是硬编码在Spine文件中的，这些骨骼的位置都是实时解算的。 
  
  这也是为什么你在官方编辑器中无法看到部分动态效果。

1. 对于这个功能, 首先拖动你想播放的SkeletonDataAssets进入Scene窗体。

![Movement_Drag](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Movement_Drag.jpg)

2. 添加组件 **Skeleton Utility**.

3. 点击 **Spawn Hierachy** -> **Follow All Bones**

![Movement_Add](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Movement_Add.jpg)

4. 点击Unity播放按钮, 应该就能看到动态效果。

## 重建 Mihoyo / Hoyoverse 网页层级

1. 对于这个功能, 请先用右键，在Hierrachy窗体里创建一个空的GameObject对象。

2. 添加组件 **Vendors Generator**

3. 对于每一个用 Vendors Decoder生成的**活动名** 文件夹，都会有一个以 **_Geo** 为后缀的json文件。

4. 拖它到 **Geometry Json** 这一栏。

![Generate_Geo](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Generate_Geo.jpg)

5. 点击 **Spawn Geometry**

6. 查看生成的层级, 通常第一个子节点，都是网页一个场景的根节点。

![Generate_Geo_Result](https://github.com/ZeroFlyFly/WaifuSpineRuntime/blob/main/ScreenShot/Generate_Geo_Result.jpg)

7. 用显示和隐藏功能挑选出你感兴趣的场景。

8. 点击Unity播放按钮，你就可以看到Spine动画，粒子效果等。

**实验性功能:** 

1. 对于每一个 **活动名** 文件夹, 都会有一个叫做 **otherResources**的子文件夹, 所有的暂不明用途文件都会下载到那里.
 
2. 里面可能有形如**json_0_Other**,**json_1_Other**...等json文件, 这些可能是动画文件, 你可以拖到 **Animation File List**列表里,再点击**Spawn Geometry**

3. 你会看到**otherResources**文件夹里，多了个Animation文件夹。

4. 在**VendorsGenerator** 对象下添加Animation组件，可以放入对应的动画文件。
 
5. 点击播放按钮，就可以看到动画效果。


# 常见问题

#### 1. 为什么我解析 Mihoyo / Hoyoverse 网页失败?

   回答: 网页解析是基于Vendors.js和Index.js。对于2021/09/28. (一周年总结，这个网页可以的)前的网页，写法已经和现在版本有很大不同。
   
   比如, 老版本中用的是bundle.js而不是index.js这2者写法有很大不同。

   要支持太老的版本，实在是比较困难，还不如另开项目重写。
   
   不过新版本的写法已经逐渐稳定趋同，新版本有更大概率可以正确解析。
   
   总之如果解析失败可以提issue，记得附上网页链接和Unity报错内容，非常感谢。
   
   但还请做好心理准备，太老的网页我确实不打算支持。

#### 2. 为什么我的Spine 透明看上去很怪?

   请阅读准备的第二点, 或者看官方文档的 [Spine-Unity-Document](https://esotericsoftware.com/spine-unity) **Alpha Texture Settings** 章节
   
   对于已经导入的Spine，上述修改不会生效，请移除出Unity后重新导入。

#### 3. 为什么我的 Mihoyo Spine 文件放进官方Spine Editor里看着动画很怪?
   
   (1) 首先, 衣服，头发，飘带，尾巴等都有可能是实时解算的, 没有硬编码写入Spine文件中。
   
   (2) 其次, 米哈游把变形动画提出来了，没有放入Attachments中，这会导致官方Spine读取不正确。

   目前我是把错误层级跳过，所以可以做到Untiy里正确读取。 

   至于后续版本支不支持修复米哈游的层级修改问题，让官方Spine也能正确读取，还在考虑当中。

# 写在最后

该仓库只包含Spine Runtime代码和一些示例工程。使用时请遵守官方Spine的License要求，并且一定要注意版权问题。 没有版权的素材一定不可用于商业用途。

如果该仓库中有侵权内容，还请告知我，我会第一时间删除对应的素材。
   

## 下面是原版Spine Runtime Read Me

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
