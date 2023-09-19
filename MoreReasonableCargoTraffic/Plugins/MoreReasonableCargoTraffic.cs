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
    [BepInPlugin("shisang_MoreReasonableCargoTraffic", "MoreReasonableCargoTraffic", "1.2.1")]
    public class MoreReasonableCargoTraffic : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(MoreReasonableCargoTraffic));
            Debug.Log("Add MoreReasonableCargoTraffic");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), "Update")]
        public static bool CargoPath_UpDate_Prefix(CargoPath __instance)
        {
            if (__instance.outputPath != null)
            {
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
                }
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
                    label_41:
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
                                        goto label_41;
                                }
                            }
                            else
                                break;
                        }
                        int num4 = index2 + (num2 == 0 ? 1 : num2);
                        if (num1 > num4)
                            num1 = num4;
                    }
                    __instance.lastUpdate = (bool)((GameMain.gameTick & 1) == 1);
                }
            }
            return false;
        }

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
            }
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
                    num8_1 = num8;
                }
                if (num9 == 10 + borrowedBuffer)//num7后找满10+borrowedBuffer个空buffer时移动的buffer数
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
                if (__instance.closed && index2 == 0 && num7 > 9)
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
                        if (num9 == 10 + borrowedBuffer)//num7后找满10+borrowedBuffer个空buffer时移动的buffer数
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
                }//index1指向最下游空格
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
                int num7 = index + 5;//num7=index1也是货物最下游
                int index2 = index - 4;//货物最上游，判断方式是因为下游不可能有其他货物，所以此处空必然连着下游连续空
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
                        if (__instance.closed && index3 == 0 && num7 > 9)
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
                                if (num10 == 10 + borrowedBuffer)//num7后找满10+borrowedBuffer个空buffer时移动的buffer数
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
