using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx;

namespace MoreReasonableCargoTraffic
{
    [BepInPlugin("shisang_MoreReasonableCargoTraffic", "MoreReasonableCargoTraffic", "1.2.3")]
    public class MoreReasonableCargoTraffic : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(MoreReasonableCargoTraffic));
            Debug.Log("Add MoreReasonableCargoTraffic");
        }

        /*
            并带不满bug修复思路:
                给线路添加一个“最后刷新帧"(lastUpdate)的概念，在每帧线路刷新逻辑的末尾更新后解线程锁，更新方式为“lastUpdate = 当前游戏帧数 % 2”
                然后，在线路尝试末端输出到另一条线路时(并入其它传送带时)，比较对方和自己的lastUpdate是否相同，因为此时自己的lastUpdated还未刷新，而上一帧结束时二者的lastUpdated是相同的
                所以若此时二者lastUpdated不同则说明输出的目标线路在这一帧已经刷新过了，那么就将输出口往下游移动一小段距离，这一小段距离的长度取决于输出目标带子的速度，蓝带为5、绿带2、黄带1
                所以当畅通时，原先的空隙后的货物前端会在这一帧会移动到我们预判的目标位置，如果货物前端并没有按带速移动，只可能是他前方堵住了，而被堵住也意味着空隙消失不再存在
                而游戏内没有记录输出目标段传送带的数据，如果再加一个数据要动到拆建传送带时的函数，所以我这边偷懒直接用二分查找并记录来每帧维护目标带的编号
                因为游戏里传送带线路中节点数据(Chunks)是按从上游到下游递增的，chunks[3*i]、chunks[3*i+1]、chunks[3*i]分别意味着第i段传送带的起始bufferIndex、buffer容量、速度
                所以在CargoPath_UpDate_Prefix()的开头我先利用维护数据二分查找输出目标的"outputChunk"，在不重新拆建的情况下每帧刷新时查找的时间复杂度是O(1),改变目标带线路结构时复杂度是O(nlogn)
                然后相比于原先的代码多出的计算量就一个跟新lastUpdate和比较lastUpdate了，从期望上来说每帧每条线路刷新时基本上可以看做就多了那么几次计算，所以对卡顿几乎没有影响
        */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), "Update")]
        public static bool CargoPath_UpDate_Prefix(CargoPath __instance)
        {
            if (__instance.outputPath != null)
            {
                //以下为改动点1
                int outputChunk = __instance.outputChunk;
                if (outputChunk > __instance.outputPath.chunkCount - 1)
                {
                    outputChunk = __instance.outputPath.chunkCount - 1;
                }
                int left = 0;
                int right = __instance.outputPath.chunkCount;
                int speed = 5;
                while (true)
                {
                    if(__instance.outputPath.chunks[outputChunk*3] > __instance.outputIndex)
                    {
                        right = outputChunk;
                        outputChunk = (left + outputChunk) / 2;
                    }
                    else if(__instance.outputPath.chunks[outputChunk * 3] + __instance.outputPath.chunks[outputChunk * 3 + 1] < __instance.outputIndex)
                    {
                        left = outputChunk + 1;
                        outputChunk = (right + outputChunk) / 2;
                    }
                    else
                    {
                        __instance.outputChunk = outputChunk;
                        speed = __instance.outputPath.chunks[outputChunk * 3 + 2];
                        break;
                    }
                    if(left == outputChunk || right == outputChunk)
                    {
                        Debug.Log("出现错误，没法找到并带节");
                    }
                }//改动点1：从上次记录点开始二分查找目标节点的id，已获得速度，不改变线路构造的情况下查找的第一个就是目标节点

                byte[] numArray = __instance.id > __instance.outputPath.id ? __instance.buffer : __instance.outputPath.buffer;
                lock (__instance.id < __instance.outputPath.id ? __instance.buffer : __instance.outputPath.buffer)
                {
                    lock (numArray)
                    {
                        int index = __instance.bufferLength - 5 - 1;
                        if (__instance.buffer[index] == (byte)250)
                        {
                            int cargoId = (int)__instance.buffer[index + 1] - 1 + ((int)__instance.buffer[index + 2] - 1) * 100 + ((int)__instance.buffer[index + 3] - 1) * 10000 + ((int)__instance.buffer[index + 4] - 1) * 1000000;
                            if (__instance.closed)
                            {
                                if (__instance.outputPath.TryInsertCargoNoSqueeze(__instance.outputIndex, cargoId))
                                {
                                    Array.Clear((Array)__instance.buffer, index - 4, 10);
                                    __instance.updateLen = __instance.bufferLength;
                                }
                            }
                            // TryInsertCargo中的第一个参数为改动点2，根据输出目标线路此帧是否以刷新来决定是否下移outputIndex(还有一些判断是为了防之后的逻辑中index超出bufferLength做的调整)
                            else if (__instance.outputPath.TryInsertCargo(__instance.lastUpdate == __instance.outputPath.lastUpdate ? __instance.outputIndex : (__instance.outputIndex + speed + 6 > __instance.outputPath.bufferLength ? __instance.outputPath.bufferLength - 6 : __instance.outputIndex + speed), cargoId))
                            {
                                Array.Clear((Array)__instance.buffer, index - 4, 10);
                                __instance.updateLen = __instance.bufferLength;
                            }
                        }
                    }
                }
            }
            else if (__instance.bufferLength <= 10)
                return false;
            lock (__instance.buffer)
            {
                for (int index = __instance.updateLen - 1; index >= 0 && __instance.buffer[index] != (byte)0; --index)
                    --__instance.updateLen;
                if (__instance.updateLen == 0)
                    return false;
                int num1 = __instance.updateLen;
                for (int index1 = __instance.chunkCount - 1; index1 >= 0; --index1)
                {
                    int index2 = __instance.chunks[index1 * 3];
                    int chunk = __instance.chunks[index1 * 3 + 2];
                    if (index2 < num1)
                    {
                        if (__instance.buffer[index2] != (byte)0)
                        {
                            for (int index3 = index2 - 5; index3 < index2 + 4; ++index3)
                            {
                                if (index3 >= 0 && __instance.buffer[index3] == (byte)250)
                                {
                                    index2 = index3 >= index2 ? index3 - 4 : index3 + 5 + 1;
                                    break;
                                }
                            }
                        }
                        int num2 = 0;
                        while (num2 < chunk)
                        {
                            int num3 = num1 - index2;
                            if (num3 >= 10)
                            {
                                int length = 0;
                                for (int index4 = 0; index4 < chunk - num2 && __instance.buffer[num1 - 1 - index4] == (byte)0; ++index4)
                                    ++length;
                                if (length > 0)
                                {
                                    Array.Copy((Array)__instance.buffer, index2, (Array)__instance.buffer, index2 + length, num3 - length);
                                    Array.Clear((Array)__instance.buffer, index2, length);
                                    num2 += length;
                                }
                                int index5 = num1 - 1;
                                while (true)
                                {
                                    if (index5 >= 0 && __instance.buffer[index5] != (byte)0)
                                    {
                                        --num1;
                                        --index5;
                                    }
                                    else
                                        break;
                                }
                            }
                            else
                                break;
                        }
                        int num4 = index2 + (num2 == 0 ? 1 : num2);
                        if (num1 > num4)
                            num1 = num4;
                    }
                }
                //改动点3每帧刷新后解线程锁前更新lastUpdate
                __instance.lastUpdate = (bool)((GameMain.gameTick & 1) == 1);
            }
            return false;
        }


        /*
            以下函数的改动与修复并带不满bug无关，只是为了修复插入环带防爆带的bug
            TryInsertCargo和TryInsertItem的逻辑类似，一个是线路输入一个是其余建筑或手塞等非线路货物输入用到的函数
            这里的改动体现在如果检测到目标是环带的话会提前检查线路末尾的10节buffer是否被占用，若有被占用的虚buffer(代码里叫borrowedBuffer)
            则向前遍历时判定可插入的条件由找到10个空buffer变成找到10+borrowedBuffer个空buffer才可发生插入判定
            实际上环带不爆带只需保证线路中永远有10个空buffer可以维持自插入时的货物转移即可，选末尾10个buffer的原因是因为这个自插入机制末尾的10个buffer和开头的10个buffer比其它部分的buffer更容易为空
            尽可能的空可以使borrowedBuffer尽可能的小，减少向前遍历时的计算量
            然后又修复了如果空隙分散的话即使空隙足够，在环带的头结点也难以插入货物的问题
            这个问题的起因是原先的逻辑在向前遍历的时候遍历到buffer[0]处就终止遍历了，所以靠近buffer[0]的插入点实际能遍历的长度很小，就容易有空隙遍历不到
            这边改成了如果检测到插入目标是环带，则在遍历到buffer[0]处后从buffer[bufferLength-10]处开始接着遍历
            直到累计遍历2880buffer或者遍历到最开始的插入点(即绕了环带一圈)为止
        */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), "TryInsertCargo")]
        public static bool CargoPath_TryInsertCargo_Prefix(CargoPath __instance, ref bool __result, int index, int cargoId)
        {
            int index1 = index + 5;
            int num1 = index - 5;
            if (index < 4 || index1 >= __instance.bufferLength)
            {
                __result = false;
                return false;
            }
            bool flag1 = false;
            while (index > num1)
            {
                if (__instance.buffer[index1] != (byte)0)
                {
                    --index;
                    --index1;
                }
                else
                {
                    flag1 = true;
                    break;
                }
            }
            if (!flag1)
            {
                __result = false;
                return false;
            }
            if (index1 + 6 < __instance.bufferLength)
            {
                int num2;
                if (__instance.buffer[num2 = index1 + 1] != (byte)0)
                {
                    index = num2 - 1 - 5;
                }
                else
                {
                    int num3;
                    if (__instance.buffer[num3 = num2 + 1] != (byte)0)
                    {
                        index = num3 - 1 - 5;
                    }
                    else
                    {
                        int num4;
                        if (__instance.buffer[num4 = num3 + 1] != (byte)0)
                        {
                            index = num4 - 1 - 5;
                        }
                        else
                        {
                            int num5;
                            if (__instance.buffer[num5 = num4 + 1] != (byte)0)
                            {
                                index = num5 - 1 - 5;
                            }
                            else
                            {
                                int num6;
                                if (__instance.buffer[num6 = num5 + 1] != (byte)0)
                                    index = num6 - 1 - 5;
                            }
                        }
                    }
                }
            }
            if (index < 4)
            {
                __result = false;
                return false;
            }
            int num7 = index + 5;
            int num8 = 0;
            int num9 = 0;
            int num8_1 = 0;
            bool flag2 = false;
            bool flag3 = false;
            int borrowedBuffer = 0;
            if (__instance.closed)
            {
                for (int i = 1; i <= __instance.bufferLength && i <= 10; i++)
                {
                    if (__instance.buffer[__instance.bufferLength - i] != (byte)0)
                    {
                        borrowedBuffer += 1;
                    }
                }
                if (num7 > __instance.bufferLength - 11)
                {
                    borrowedBuffer += num7 - __instance.bufferLength + 11;
                }
            }//判断目标带是环带时，通过遍历末10节buffer中非空buffer数来决定borrowedBuffer的大小\
            for (int index2 = num7; index2 >= num7 - 2880 && index2 >= 0; --index2)
            {
                if (__instance.buffer[index2] == (byte)0)
                {
                    ++num9;
                    if (!flag2)
                        ++num8;
                }
                else
                {
                    flag2 = true;
                    if (num8 < 1)
                    {
                        __result = false;
                        return false;
                    }
                }
                if (num9 == 10)
                {
                    num8_1 = num8;//num8_1是临时用于记录num8的值的，num8是num7(插入点)后的连续空隙数，num9是累计空隙数，如果num8＞10的话会导致之后的逻辑出问题，所以这边我用这句逻辑保证连续空隙数>10的时候num8也不会大于10
                }
                if (num9 == 10 + borrowedBuffer)//插入点后找满10+borrowedBuffer个空buffer时移动的buffer数才判定为可插入
                {
                    num8 = num8_1;
                    num9 = 10;
                    if (num8 >= 10)
                    {
                        __instance.InsertCargoDirect(index, cargoId);
                        __result = true;
                        return false;
                    }
                    flag3 = true;
                    break;
                }
                if (__instance.closed && index2 == 0 && num7 > 9 && num7 < __instance.bufferLength - 11)//遍历到环带buffer[0]处但未遍历2880buffer时的逻辑
                {
                    for (index2 = __instance.bufferLength - 11; __instance.bufferLength - index2 >= 2890 - num7 && index2 > num7; --index2)
                    {
                        if (__instance.buffer[index2] == (byte)0)
                        {
                            ++num9;
                        }
                        if (num9 == 10)
                        {
                            num8_1 = num8;
                        }
                        if (num9 == 10 + borrowedBuffer)
                        {
                            num8 = num8_1;
                            num9 = 10;
                            if (num8 >= 10)
                            {
                                __instance.InsertCargoDirect(index, cargoId);
                                __result = true;
                                return false;
                            }
                            flag3 = true;
                            break;
                        }
                    }
                    break;
                }
            }
            if (flag3)
            {
                int num10 = num9 - num8;
                int num11 = num7 - num8 + 1;
                for (int sourceIndex = index - 4; sourceIndex >= num7 - 2880 && sourceIndex >= 0; --sourceIndex)
                {
                    if (__instance.buffer[sourceIndex] == (byte)246)
                    {
                        int num12 = 0;
                        for (int index3 = sourceIndex - 1; index3 >= num7 - 2880 && index3 >= 0 && num12 < num10 && __instance.buffer[index3] == (byte)0; --index3)
                            ++num12;
                        if (num12 > 0)
                        {
                            Array.Copy((Array)__instance.buffer, sourceIndex, (Array)__instance.buffer, sourceIndex - num12, num11 - sourceIndex);
                            num10 -= num12;
                            num11 -= num12;
                            sourceIndex -= num12;
                        }
                    }
                }
                if (num10 == 0)
                {
                    __instance.InsertCargoDirect(index, cargoId);
                    __result = true;
                    return false;
                }
                Assert.CannotBeReached("断言失败：插入货物逻辑有误");
            }
            __result = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), "TryInsertItem")]
        public static bool CargoPath_TryInsertItem_Prefix(CargoPath __instance, ref bool __result, int index, int itemId, byte stack, byte inc)
        {
            lock (__instance.buffer)
            {
                int index1 = index + 5;
                int num1 = index - 5;
                if (index < 4 || index1 >= __instance.bufferLength)
                {
                    __result = false;
                    return false;
                }
                bool flag1 = false;
                while (index > num1)
                {
                    if (__instance.buffer[index1] != (byte)0)
                    {
                        --index;
                        --index1;
                    }
                    else
                    {
                        flag1 = true;
                        break;
                    }
                }
                if (!flag1)
                {
                    __result = false;
                    return false;
                }
                if (index1 + 6 < __instance.bufferLength)
                {
                    int num2;
                    if (__instance.buffer[num2 = index1 + 1] != (byte)0)
                    {
                        index = num2 - 1 - 5;
                    }
                    else
                    {
                        int num3;
                        if (__instance.buffer[num3 = num2 + 1] != (byte)0)
                        {
                            index = num3 - 1 - 5;
                        }
                        else
                        {
                            int num4;
                            if (__instance.buffer[num4 = num3 + 1] != (byte)0)
                            {
                                index = num4 - 1 - 5;
                            }
                            else
                            {
                                int num5;
                                if (__instance.buffer[num5 = num4 + 1] != (byte)0)
                                {
                                    index = num5 - 1 - 5;
                                }
                                else
                                {
                                    int num6;
                                    if (__instance.buffer[num6 = num5 + 1] != (byte)0)
                                        index = num6 - 1 - 5;
                                }
                            }
                        }
                    }
                }
                if (index < 4)
                {
                    __result = false;
                    return false;
                }
                int num7 = index + 5;
                int index2 = index - 4;
                int borrowedBuffer = 0;
                if (__instance.closed)
                {
                    for (int i = 1; i <= __instance.bufferLength && i <= 10; i++)
                    {
                        if (__instance.buffer[__instance.bufferLength - i] != (byte)0)
                        {
                            borrowedBuffer += 1;
                        }
                    }
                    if (num7 > __instance.bufferLength - 11)
                    {
                        borrowedBuffer += num7 - __instance.bufferLength + 11;
                    }
                }
                if (index2 - borrowedBuffer < 0)
                {
                    __result = false;
                    return false;
                }
                if (__instance.buffer[index2] == (byte)0 && __instance.buffer[index2 - borrowedBuffer] == (byte)0)
                {
                    __instance.InsertItemDirect(index, itemId, stack, inc);
                    __result = true;
                    return false;
                }
                int num8 = num7 - 2880;
                if (num8 < 0)
                    num8 = 0;
                int num9 = 0;
                int num9_1 = 0;
                int num10 = 0;
                bool flag2 = false;
                bool flag3 = false;
                for (int index3 = num7; index3 >= num8; --index3)
                {
                    if (__instance.buffer[index3] == (byte)0)
                    {
                        ++num10;
                        if (!flag2)
                            ++num9;
                        if (num10 == 10)
                        {
                            num9_1 = num9;
                        }
                        if (num10 == 10 + borrowedBuffer)
                        {
                            if (num9_1 == 10)
                            {
                                __instance.InsertItemDirect(index, itemId, stack, inc);
                                __result = true;
                                return false;
                            }
                            num10 = 10;
                            num9 = num9_1;
                            flag3 = true;
                            break;
                        }
                        if (__instance.closed && index3 == 0 && num7 > 9 && num7 < __instance.bufferLength - 11)
                        {
                            for (index3 = __instance.bufferLength - 10; __instance.bufferLength - index3 >= 2890 - num7 && index3 > num7; --index3)
                            {
                                if (__instance.buffer[index3] == (byte)0)
                                {
                                    ++num10;
                                }
                                if (num10 == 10)
                                {
                                    num9_1 = num9;
                                }
                                if (num10 == 10 + borrowedBuffer)
                                {
                                    num9 = num9_1;
                                    num10 = 10;
                                    if (num9 >= 10)
                                    {
                                        __instance.InsertItemDirect(index, itemId, stack, inc);
                                        __result = true;
                                        return false;
                                    }
                                    flag3 = true;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    else
                    {
                        flag2 = true;
                        if (num9 < 1)
                        {
                            __result = false;
                            return false;
                        }
                        if (__instance.buffer[index3] == byte.MaxValue)
                            index3 -= 9;
                    }
                }
                if (flag3)
                {
                    int num11 = num10 - num9;
                    int num12 = num7 - num9 + 1;
                    for (int sourceIndex = index2; sourceIndex >= num8; --sourceIndex)
                    {
                        if (__instance.buffer[sourceIndex] == (byte)246)
                        {
                            int num13 = 0;
                            for (int index4 = sourceIndex - 1; index4 >= num8 && num13 < num11 && __instance.buffer[index4] == (byte)0; --index4)
                                ++num13;
                            if (num13 > 0)
                            {
                                Array.Copy((Array)__instance.buffer, sourceIndex, (Array)__instance.buffer, sourceIndex - num13, num12 - sourceIndex);
                                num11 -= num13;
                                num12 -= num13;
                                sourceIndex -= num13;
                            }
                        }
                    }
                    if (num11 == 0)
                    {
                        __instance.InsertItemDirect(index, itemId, stack, inc);
                        __result = true;
                        return false;
                    }
                    Assert.CannotBeReached("断言失败：插入货物逻辑有误");
                }
            }
            __result = false;
            return false;
        }

    }
}
