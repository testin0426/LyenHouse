---
title: ねんの部屋
status: in-development
summary: ねんの部屋
tags: [gamejam, narrative, exploration]
# cover: /images/sugaricon.jpg
rating: all-ages
order: 0
---

##

《晴雨》中怜の部屋

## 聚焦方向：
主氛围、物件、空间设计、排版、
涉及：自定义renderpipeline多材质卡渲表现渲染、性能优化，总体为为角色设定、剧情服务。
玩法： 简化版vrchat模式交互
引擎选取：unity

## 核心意象
：雨（雨、重中之重。） 元素、意象

<p><strong>最终vrchat参考</strong></p>
<video controls preload="none" playsinline width="100%" style="max-width:960px;">
  <source src="video/2026-09-2423-50-15.mp4" type="video/mp4">
  你的浏览器不支持内嵌视频，<a href="video/2026-09-2423-50-15.mp4">点此播放/下载</a>
</video>

## 9.24 

今晚熬夜 收集好参考视频、vrchat参考、探索下上限。

- 雨痕玻璃 [完成]
- 镜子交互测试: UI 只有开/关 [无。有思路，C#沟通客户端Rt传来传去很麻烦]
- 相机交互测试 [半完成，有些问题]

总结： 完成度有点差，原先钢琴靠近是有每个音的声音的。不知为什么录制时又没有了。

<p><strong>9.24第一天探索demo</strong></p>
<video controls preload="none" playsinline width="100%" style="max-width:960px;">
  <source src="video/Movie_001.mp4" type="video/mp4">
  你的浏览器不支持内嵌视频，<a href="video/Movie_001.mp4">点此播放/下载</a>
</video>

<p><strong>理论上可以实现的</strong></p>
<video controls preload="none" playsinline width="100%" style="max-width:960px;">
  <source src="video/2026-09-2503-18-26.mp4" type="video/mp4">
  你的浏览器不支持内嵌视频，<a href="video/2026-09-2503-18-26.mp4">点此播放/下载</a>
</video>

总结: 
1. UI交互问题不解决其他大概会被拖死。考虑摇人。需求就是上面的部分。唉我真服了我本来策划能力就不行我也讨厌策划我真的是。要我我真的一个交互都不想做。如果让我全删那就保留就我今天检测碰撞做的拿起放下算了。。要不就这样算了？
2. 做项目需要找人sp画贴图。考虑摇人。(而且还要根据家具量决定。)需要会用sp，看懂shader，材质选择能力（？不过这样好像就得默认知道ao、gbuffer一系列rgb合并等各种）。能自己unity调试我谢天谢地。

## 9.25
室内设计看书补课进度可能得加，也许只留给我一两天不到时间看书。后面的games202相关计划好像都用不到了、学习与项目时间得协调是个问题，学习为重，学习为重。因为当务之急貌似还是ui相关问题。。。我草了我恨ui一辈子每次都被ui拖死我也真是

室内平面和空间划分学到很多东西...

问题: 钢琴尺寸等相关，空间要重新划分
空间划分完后细节填入等，墙壁->厚度->纹理 透明部分->书柜
影子问题。。。阴影。。

<a href="image/image-4.png" target="_blank" rel="noopener"><img src="image/image-4.png" alt="design" style="max-width:100%;border:1px solid #ddd;border-radius:6px;"></a>

### 说明

1. 文件夹 nLDK L起居室 D餐厅 K厨房划分 n房间(([《室内空间设计手册》](https://zlib.bz/book/rEMX0p5W1m/%E5%AE%A4%E5%86%85%E7%A9%BA%E9%97%B4%E8%AE%BE%E8%AE%A1%E6%89%8B%E5%86%8C.html)))
2. 我的一些设计思路: 一些墙（如设计图浅灰+深灰的遮挡作用，浅灰色（以及demo里透明材质部分是用作比较大的书架、架子，没有完全隔断但依然起到空间分割作用））
3. D，toilet部分项目比赛应该不会做到。

但是项目应该只聚焦起居室 + 卧室。封闭好空间结束。
问题:还是有些空间划分得有问题。和钢琴尺寸对不上（由于我钢琴还没来得及放dcc导致。。所以上面设计图尺寸也不太对（因为同时在引擎里面调整地编手感这一块。。奇幻。）

<p><strong>9.25 demo process</strong></p>
<video controls preload="none" playsinline width="100%" style="max-width:960px;">
  <source src="video/2026-09-2518-19-48.mp4" type="video/mp4">
  你的浏览器不支持内嵌视频，<a href="video/2026-09-2518-19-48.mp4">点此播放/下载</a>
</video>

<p><strong>shader parameter adjust</strong></p>
<video controls preload="none" playsinline width="100%" style="max-width:960px;">
  <source src="video/2026-09-2518-12-56.mp4" type="video/mp4">
  你的浏览器不支持内嵌视频，<a href="video/2026-09-2518-12-56.mp4">点此播放/下载</a>
</video>

+ 天空盒需要精修: 加底层模糊照片
+ 确定好范围后再->家具选取、摆放
+ 钢琴材质贴图可以外包了

不过这样看起来只能说在完全超计划完成但不能达到很好（80分）的阶段感觉。。

## 9.26

确定好范围后、墙壁厚度设置好再->

今天需要发布晴雨第二章内容。继续室内设计看书补课。边看书边思考去划分好空间区域、设计室内引导、家具摆放室内（这个得划定区域，应该不会很大。我估计跟宿舍差不多的大小（参考一些vrchat那种室内小地图。

音乐需求在稍有原型后分发

## 9.27


交互相关
最基础: 
拿起放下(完成) -> 拍照交互 -> 冰箱复杂动画状态机交互
钢琴靠近有声音(完成) 
镜子(完成) -> UI





（9.28-10.1）
tinf: 划分好空间区域、设计室内引导、家具摆放（根据27号调整）
挑选可交互物品（根据明天情况看涉及diffuse、transprent、half-transparent中）整合到blender中
挑选可交互物品这中间我可以ai工具一键上一些功能、但是资产导入导出需要筛选模型优化调整相关，还有shader改写。这个我打算外包出去。但我不知道是到时候找人还是，我有个朋友是可以提前和她打个招呼来着。

(10.1-10.3)（根据27号后实时调整）

10.8日前交付一首音乐即可

室内（这个得划定区域，应该不会很大。我估计跟一个宿舍差不多的大小（参考一些vrchat地图。）参考视频：
音乐参考：


## 贴图制作规范

参考sp材质
每种家具需要一张rgba的lightmap贴图(.tga)
<a href="image/image.png" target="_blank" rel="noopener"><img src="image/image.png" alt="sp示例" style="max-height:180px;border:1px solid #ddd;border-radius:6px;"></a> rgba各为灰度
lightmap r: ao
<a href="image/image-1.png" target="_blank" rel="noopener"><img src="image/image-1.png" alt="ao" style="max-width:100%;border:1px solid #ddd;border-radius:6px;"></a>
lightmap g: details（类似绒毛相关）

ligtmap b: high（高光，是否被光点亮）
<a href="image/image-2.png" target="_blank" rel="noopener"><img src="image/image-2.png" alt="high" style="max-width:100%;border:1px solid #ddd;border-radius:6px;"></a>
lightmap a: 金属度体现(0 or 1金属，0.5非金属)
<a href="image/image-3.png" target="_blank" rel="noopener"><img src="image/image-3.png" alt="金属程度" style="max-width:100%;border:1px solid #ddd;border-radius:6px;"></a>

进阶: uv孤岛整合打包和烘焙
### Painting recipes (normalized 0.0–1.0)

| Material | R | G | B | A | Notes |
|---|---|---|---|---|---|
| Cotton / wool / matte fabric | 1.0 | ~0.43–0.51 | 0.0 | non-metal ID (≈0.52) | No specular. Optionally paint R darker in folds/creases (AO). |
| Denim | 1.0 | ~0.39–0.43 | ~0.06–0.12 | non-metal | Slightly wider shadow, faint highlight. |
| Satin / silk | 1.0 | ~0.47–0.50 | ~0.24–0.39 | non-metal | Broad, soft specular. |
| Leather | ~0.90 | ~0.35–0.43 | ~0.35–0.51 | non-metal | Narrow-ish highlight; raise `_SpecularExpon` if too broad. |
| Latex / PVC (very glossy) | 1.0 | ~0.50 | 1.0 | non-metal | Strongest non-metal highlight, but `_SpecularKsNonMetal` is a global property (default 0.04). |
| **Metal** | 1.0 | ~0.50 | **1.0** | **metal ID (A = 0 or 1)** | `metallic` picks `blinnPhong * B * _SpecularKsMetal * baseColor` → highlight tinted by the base color. Paint the metal color in the base color map. |

ramp逻辑甚至可忽略。

回头我应该过两天是规定空间、排版家具、确定家具后再有限个家具美术任务外包出去如此。？

## 相关描写

<details markdown="1">
<summary>第一段摘取自 第一章-困在记忆闪帧 上</summary>

某天早上，我很困，外面在下雨，空气中氤氲着水汽，屋子里也洋溢着潮湿厚重的味道....天是青灰色的，要不是手表在枕边一闪一闪，我都没意识到已经白天了。

不想起床....下雨天最好睡觉了。

但某人貌似...从昨天开始就一副「今天我要告诉你一个好消息，但是个秘密」的神奇状态，以及还「无意识」在我面前排出了几张皱巴巴的五线谱，还特意补了一句「只说给你听」。

自己也就不知觉滚下床、做好洗漱相关、关掉空气净化器后看了眼苏阿姨在电话手表里留言后就出门了。

水滴，亦或是说水痕在窗玻璃上爬行、蔓延。我时常通过这扇玻璃看外面，平时，从这里就能俯瞰到我们小学，能看到白霜把球和球拍打上去的那棵树.....而今天什么也看不见，青灰色的世界。

「哗....————....」

靴子在斑驳砖块和泥水洼间高高低低踏着着，踩过散落一地的、如同粉末一样的桂花花瓣，即使这样，它们的香气仍旧浓郁。经过深水洼时，我小心拎起雨衣，又放下。再避开赶路的人、飞驰的车.....我盯着铁门栏外的方向。

远远地，我终于看见白霜，她在远处转她伞，那把红色的印有卡通图案的伞，在青灰色的天际与人流间极度明艳，她搓着抛起来，然后再接住，非常轻盈。其实她有时也没接住，她没穿雨衣，好像完全无所谓，但我不喜欢被淋着湿漉漉的...于是我立刻冲过去，想提醒她。

（....中间省略一部分。然后接续）

于是我也有些好奇，我也就向阿姨提出这些，她倒一副「随便我」的态度，然后继续她的各种出差。

自从家里凭空多出来一架黑色巨物后，需要我改掉老习惯，滚下床后得绕开它走，要么远远地观察它，非常后悔当初提出了这件事情。但是这个陌生的大东西不像用完的修正带可以随便扔掉。它的颜色也很单调。除此外，每周日下午，我还要应付一个无聊的陌生人。

所以，「你可以来我家弹钢琴。」我向她伸手，指向我家方向。

那应该是白霜第一次来我家玩吧，现在看来，那时的她大概不会喜欢我家这样低饱和的颜色、更不会喜欢这样毫无生活气息的....家？

「哇.....」她平息凝神，非常小心翼翼，「我穿哪个...?」她问我，指了指鞋柜。

「随便，穿哪个都行。」

结果她没有穿鞋，穿着袜子小心移动着，向我招手示意：「你家有大人在吗....」

我摇摇头。

她好像松了一口气，一蹦一跳向钢琴的方向前去。

「好哎....」她迫不及待打开琴盖，翻出她包里的谱子，高高低低地摁着琴键。

不连续的、间断的乐曲想起，融化在微微潮湿的空气中，水中溶解了糖，空间并没有什么变化，但又貌似填上了某种空隙。

</details>

<details markdown="1">
<summary>第二段摘取至第二章 如果那么容易被替代</summary>

以下是怜梦境中家里的场景：

白霜突然抓紧我的手，我趔趄两步、差点摔倒，她拉着我，向家里那些落地玻璃冲去，我立刻闭眼。

……预想的疼痛并没有发生，随之而来的只有哗啦哗啦玻璃破碎的声音、自然下坠，自由落体的“感觉”、以及渐渐减速、又滑翔向上升起、向前远远跃去的，灰色世界中飞翔的白霜。

</details>

这个角色大概的生活习惯是比较困、爱睡、..习性像猫、家里不养宠物
