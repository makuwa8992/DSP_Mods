using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx;

namespace LongerBelts
{
    [BepInPlugin("shisang_LongerBelts", "LongerBelts", "0.0.1")]
    public class LongerBelts : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(LongerBelts));
            Debug.Log("Add LongerBelts");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetAuxData), "SnapLineNonAlloc")]
        public static bool PlanetAuxData_SnapLineNonAlloc_Prefix(
            PlanetAuxData __instance, 
            Vector3 begin,
            Vector3 end,
            int path,
            bool geodesic,
            bool begin_flat,
            Vector3[] snaps,
            ref int __result)
        {
            int num1 = 0;
            int num2 = snaps.Length - 10;
            if (num2 == 0)
			{
                __result = 0;
                return false;
			}
            Array.Clear((Array)snaps, 0, snaps.Length);
            if (__instance.activeGrid == null)
                geodesic = true;
            if (!geodesic)
            {
                num1 = __instance.activeGrid.SnapLineNonAlloc(begin, end, path, snaps);//段数
            }
            else
            {
                //测地线模式线路节点坐标生成改动处
                //int layers = Mathf.RoundToInt((float)((double)Mathf.Abs(begin.magnitude - end.magnitude) / 1.33333325386047 * 2.0 + 1.0));//num13=(起止点高度差/1.33/0.5)+1（判断层数）
                float max_radius = begin.magnitude > end.magnitude ? begin.magnitude : end.magnitude;
                Vector3 beginNormalized = begin.normalized;
                Vector3 endNormalized = end.normalized;
                double distance = Mathf.Acos(Vector3.Dot(beginNormalized, endNormalized)) * max_radius / 2.1;
                int num10 = distance > 0.1 ? (int)distance + 1 : 0;
                if (num10 == 0)
                {
                    snaps[num1++] = beginNormalized;
                }
                else
				{
                    /*
                    if(layers > 1)
					{
                        num10 = num10 > layers + (begin_flat ? 0 : 1) ? num10 : layers + (begin_flat ? 0:1);
					}//防止端点不水平，但可能会在尝试接线的时候让带子无意义地变多
                    */
                    for (int index = 0; index <= num10 && num1 < num2; ++index)
                    {
                        float t = (float)index / (float)num10;
                        snaps[num1++] = Vector3.Slerp(beginNormalized, endNormalized, t).normalized;
                    }
                }
            }
            float magnitude1 = begin.magnitude;
            float magnitude2 = end.magnitude;
            if (num1 == 1 && (double)Mathf.Abs(magnitude1 - magnitude2) > 1.0 / 1000.0)
            {
                float num11 = (float)((double)Mathf.Max(0.0f, Mathf.Floor((float)(((double)magnitude2 - (double)__instance.planet.radius) / 1.33333325386047))) * 1.33333325386047 + (double)__instance.planet.radius + 0.200000002980232);
                int num12 = 2;
                snaps[1] = snaps[0] * num11;
                snaps[0] = snaps[0] * magnitude1;
                __result = num12;
                return false;
            }
            int num13 = Mathf.RoundToInt((float)((double)Mathf.Abs(magnitude2 - magnitude1) / 1.33333325386047 * 2.0 + 1.0));//num13=(起止点高度差/1.33/0.5)+1（判断层数）
            int num14 = (num13 < num1 ? num13 : num1) - 1;//垂直段数和水平段数的较小值
            if (num14 <= 0)
                num14 = 1;
            int num15 = num1 > num14 + 1 & begin_flat ? 1 : 0;//端点水平判定
            for (int index = 0; index < num1; ++index)
            {
                float num16 = num14 > 0 ? (float)(index - num15) / (float)num14 : 0.0f;//水平高度差
                if ((double)num16 < 0.0)
                    num16 = 0.0f;
                if ((double)num16 > 1.0)
                    num16 = 1f;
                float num17 = (float)((double)magnitude1 * (1.0 - (double)num16) + (double)magnitude2 * (double)num16);
                snaps[index] = snaps[index] * num17;
            }
            __result = num1;
            return false;
		}
    }
}
