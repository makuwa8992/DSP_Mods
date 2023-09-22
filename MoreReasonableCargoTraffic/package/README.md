# MoreReasonableCargoTraffic

#### Revise the logic of the conveyor belts to make them more Reasonable.
#### 对传送带的逻辑进行改进以更符合直觉

## Updates

* 0.0.1
  * Fix the bug preventing blue belts from reaching maximum throughput when merging.
  * 修复蓝带并带时可能无法满带的bug

* 1.0.0
  * Fix the bug that can cause a stoppage when inserting a closed loop of belts and reaching maximum capacity.
  * 修复插入环带使其达到最大容量时可能导致环带停转的bug

* 1.0.1
  * Because version 1.0.0 has a nasty bug, now rollback to version 0.0.1
  * 因为1.0.0版本有恶性bug(并入环带时可能发生数组越界)，所以此处回退到0.0.1，请等待修复

* 1.1.0
  * Fix the bug that could cause a stoppage when inserting a closed loop of belts and reaching maximum capacity.
  * 修复插入环带使其达到最大容量时可能导致环带停转的bug
  * Fix the bug that when inserting a lower belt which is connected to a higher belt, it may input with higher throughput than the lower belt's maximum throughput
  * 修复插入一节后面跟着高级传送带的低级传送带时可能导致超出低级传送带运力的输入量的bug

* 1.2.0
  * Fix the bug that when inserting a very short lower belt which is connected to a higher belt, the cargos may be repeated pushed and led to a countercurrent
  * 修复插入一节后面跟着高级传送带的超短低级传送带时可能反复推挤插入而引起货物逆流的bug

* 1.2.1
  * Fixed a bug where two branches could not reach maximum flow when merging into a short main path and entering a building such as a logistics tower, but the bug fixed in 1.2.0 can be repeated again
  * 修复当两条支路并入一节短主路后并进入物流塔等建筑时无法达到最大流量的bug,但是1.2.0版本修复的bug可以再次复现

* 1.2.2
  * Reduce some unnecessary operations
  * 减少一些不必要的运算

* 1.2.3
  * Fixed the bug that also could cause a stoppage when inserting the tail node of a closed loop of belts
  * 修复插入环带尾节点时仍可能导致爆带插入的bug
  * Add source URL
  * 加入源码网址

## Future versions(Maybe)
*  Reduce the computational overhead for fixing the loop belt stalling bug.(Currently, in typical scenarios, there isn't much additional computation involved. Even in the case of heavy use of closed loop of belts, the expected extra computation load should not exceed 1%. However, it's important to note that these computations are not entirely avoidable. There's an alternative method that involves making changes to the structure of the "CargoPath Class", which would affect the compatibility of save files with and without this mod. Therefore, I don't plan to make any changes until I acquire more modding skills.

## 未来可能的更新（但我鸽子不一定做）
* 降低为了修复环带停转bug的额外代码的计算量(目前来说如果没有非常特殊的情况，则基本没有太多额外运算.即使是在大量使用环带的情况下，额外计算量的期望应该也不会超过1%。不过这些运算并非没法避免，但由于我的另一种方法需要动到传送带线路类结构中涉及存读档的部分，可能会导致有此mod和无此mod的存档不兼容的问题，所以在我学会更多的mod制作技巧之前应该不会做出改动)

## Usage

Place the 'plugins' and 'patchers' folders in the BepInEx directory or install them through a mod manager.

## 使用说明

将plugins和patchers文件夹放到BepInEx路径下或通过mod管理器安装