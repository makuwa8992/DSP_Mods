using System;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;

namespace LongerBelts
{
    [BepInPlugin("shisang_LongerBelts", "LongerBelts", "1.1.0")]

    public class LongerBelts : BaseUnityPlugin
    {
        private bool DisplayingWindow = false;
        // 启动按键
        private ConfigEntry<KeyboardShortcut> SettingWindow{ get; set; }
        private Rect windowRect = new Rect(200, 200, 550, 300);
        static public bool shortest_unlimit = false;
        static public float current_distance = 1.9f;
        static public float minimum_distance = 0.400001f;
        private float maxmum_distance = 2.302172f;
        static public int pathMode = 0;
        private string[] pathModeStrings = { "原版升降逻辑", "阿基米德螺线型升降(端点不水平，常规游戏需先在两端拉好水平带\n建议配合如建筑铺设无条件等放宽传送带铺设条件的功能使用)"};

        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(LongerBelts));
            Debug.Log("Add LongerBelts");
        }
        void Start()
        {
            SettingWindow = Config.Bind("打开窗口快捷键", "Key", new KeyboardShortcut(KeyCode.Alpha3, KeyCode.R));
            Debug.Log("快捷键已启用");
        }

        void Update()
        {
            if (SettingWindow.Value.IsDown())
            {
                DisplayingWindow = !DisplayingWindow;
            }
        }

        private void OnGUI()
        {
            GUI.backgroundColor = Color.gray;
            if (DisplayingWindow)
            {
                windowRect = GUI.Window(20231008, windowRect, SetLongerBelts, "LongerBelts");
            }
        }

        public void SetLongerBelts(int winId)
        {
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
            GUILayout.BeginHorizontal();
            GUILayout.Label("最大间距设置(注意单位为米而非格)", GUILayout.Width(260f));
            current_distance = GUILayout.HorizontalSlider(current_distance, minimum_distance, maxmum_distance, GUILayout.Width(100f));
            string input_distance = GUILayout.TextField(current_distance.ToString(), GUILayout.Width(140f));
            float temp_distance;
			if (float.TryParse(input_distance,out temp_distance))
			{
                if(temp_distance < maxmum_distance && temp_distance > minimum_distance)
			    {
                    current_distance = temp_distance;
                }
			}
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("传送带路径");
            pathMode = GUILayout.SelectionGrid(pathMode, pathModeStrings, 1, "toggle");
            GUILayout.Label("");
            GUILayout.Label("下列功能慎用!");
            shortest_unlimit = GUILayout.Toggle(shortest_unlimit, "勾选启用弱约束间距输入框(常规游戏中会出现传送带过短或过长等错误\n即使使用无条件铺设也可能触发特殊bug,造出的蓝图也未必能用)");
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("弱约束间距输入框(配合铺设无条件作弊、测试用):", GUILayout.Width(360f));
            string unlimited_input_distance = GUILayout.TextField(current_distance.ToString(), GUILayout.Width(140f));
            if (float.TryParse(unlimited_input_distance, out temp_distance) && shortest_unlimit)
            {
                if (temp_distance < 0.001f) temp_distance = 0.001f;
                if(temp_distance > 999f) temp_distance = 999f;
                current_distance = temp_distance;
            }
            GUILayout.EndHorizontal();
            EatInputInRect(windowRect);
        }
        public static void EatInputInRect(Rect eatRect)
        {
            if (!(Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))) //Eat only when left-click
                return;
            if (eatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
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
                if (path == 2)
                {
                    //额外测地线模式线路节点坐标生成改动处
                    float max_radius = begin.magnitude > end.magnitude ? begin.magnitude : end.magnitude;
                    Vector3 beginNormalized = begin.normalized;
                    Vector3 endNormalized = end.normalized;
                    float nodes_counts;
                    float geodesic_distance = Mathf.Acos(Vector3.Dot(beginNormalized, endNormalized)) * max_radius;
                    int num10;
                    if (LongerBelts.pathMode == 1)
					{
                        float beginMagnitude = begin.magnitude;
                        float endMagnitude = end.magnitude;
                        float delta_height = Mathf.Abs(endMagnitude - beginMagnitude);//起止点高度差
                        nodes_counts = Mathf.Sqrt( geodesic_distance * geodesic_distance + delta_height * delta_height) / LongerBelts.current_distance;
                        if (!LongerBelts.shortest_unlimit && Mathf.Sqrt(geodesic_distance * geodesic_distance + delta_height * delta_height) / ((int)nodes_counts + 1) < LongerBelts.minimum_distance)//实际建造时传送带距离过短判定是<=0.4m
                        {
                            nodes_counts = Mathf.Sqrt(geodesic_distance * geodesic_distance + delta_height * delta_height) / LongerBelts.minimum_distance - 1;
                        }
                        num10 = nodes_counts > 0.1 ? (int)nodes_counts + 1 : 0;
                        if (num10 == 0)
                        {
                            snaps[num1++] = beginNormalized;
                        }
                        else
                        {
                            for (int index = 0; index <= num10 && num1 < num2; ++index)
                            {
                                float t = (float)index / (float)num10;
                                snaps[num1++] = Vector3.Slerp(beginNormalized, endNormalized, t).normalized;
                            }
                        }
                        for (int index = 0; index < num1; ++index)
                        {
                            snaps[index] *= beginMagnitude * (float)(num1 - 1 - index) / (float)(num1 - 1) + endMagnitude * (float)index / (float)(num1 - 1);
                        }
                        __result = num1;
                        return false;
					}
                    nodes_counts = geodesic_distance / LongerBelts.current_distance;
                    if(!LongerBelts.shortest_unlimit && Mathf.Acos(Vector3.Dot(beginNormalized, endNormalized)) * (begin.magnitude < max_radius ? begin.magnitude : end.magnitude) / ((int)nodes_counts + 1) < LongerBelts.minimum_distance)//实际建造时传送带距离过短判定是<=0.4m
					{
                        nodes_counts = (Mathf.Acos(Vector3.Dot(beginNormalized, endNormalized)) * (begin.magnitude < max_radius ? begin.magnitude : end.magnitude) / LongerBelts.minimum_distance) - 1;
                    }
                    num10 = nodes_counts > 0.1 ? (int)nodes_counts + 1 : 0;
                    if (num10 == 0)
                    {
                        snaps[num1++] = beginNormalized;
                    }
                    else
                    {
                        for (int index = 0; index <= num10 && num1 < num2; ++index)
                        {
                            float t = (float)index / (float)num10;
                            snaps[num1++] = Vector3.Slerp(beginNormalized, endNormalized, t).normalized;
                        }
                    }
                }
                else
                {
                    VectorLF3 vectorLf3_1 = (VectorLF3)begin;
                    VectorLF3 vectorLf3_2 = (VectorLF3)end;
                    VectorLF3 vectorLf3_3 = vectorLf3_2 - vectorLf3_1;//end-begin
                    VectorLF3 normalized1 = vectorLf3_1.normalized;
                    vectorLf3_2 = vectorLf3_2.normalized;
                    VectorLF3 normalized2 = vectorLf3_3.normalized;//包含高度差的normalized
                    double num3 = __instance.activeGrid != null ? (double)__instance.activeGrid.CalcLocalGridSize((Vector3)normalized1, (Vector3)normalized2) : Math.PI / 500.0;
                    float num4 = __instance.activeGrid != null ? __instance.activeGrid.CalcLocalGridSize((Vector3)(normalized1 * 0.7 + vectorLf3_2 * 0.3).normalized, (Vector3)normalized2) : (float)Math.PI / 500f;
                    float num5 = __instance.activeGrid != null ? __instance.activeGrid.CalcLocalGridSize((Vector3)(normalized1 * 0.3 + vectorLf3_2 * 0.7).normalized, (Vector3)(-normalized2)) : (float)Math.PI / 500f;
                    float num6 = __instance.activeGrid != null ? __instance.activeGrid.CalcLocalGridSize((Vector3)vectorLf3_2, (Vector3)(-normalized2)) : (float)Math.PI / 500f;
                    double num7 = (double)num4;
                    float num8 = (float)((num3 + num7 + (double)num5 + (double)num6) / 4.0);
                    if ((double)num8 < 0.00400000018998981)
                        num8 = 0.004f;
                    VectorLF3 vectorLf3_4 = vectorLf3_2 - normalized1;
                    double num9;
                    if ((num9 = vectorLf3_4.magnitude) > 0.01)
                        num9 = Math.Acos(VectorLF3.Dot(normalized1, vectorLf3_2));
                    int num10 = Mathf.RoundToInt((float)num9 / num8);
                    if (num10 == 0)
                    {
                        snaps[num1++] = (Vector3)normalized1;
                    }
                    else
                    {
                        for (int index = 0; index <= num10 && num1 < num2; ++index)
                        {
                            float t = (float)index / (float)num10;
                            snaps[num1++] = Vector3.Slerp((Vector3)normalized1, (Vector3)vectorLf3_2, t).normalized;
                        }
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        public static bool BuildTool_Path_DeterminePreviews_Prefix(BuildTool_Path __instance)
        {
            __instance.waitForConfirm = false;
            bool flag1 = false;
            if (VFInput._liftBeltsHeight)
                ++__instance.altitude;
            if (VFInput._reduceBeltsHeight)
                --__instance.altitude;
            if (VFInput._beltsZeroKey)
                __instance.altitude = 0;
            if (__instance.altitude > 60)
                __instance.altitude = 60;
            else if (__instance.altitude < 0)
                __instance.altitude = 0;
            if (__instance.factory.entityCount < 400 && __instance.gameData.factoryCount == 1)
                __instance.actionBuild.model.promptText = "传送带建造提示0".Translate();
            if (__instance.controller.cmd.stage == 0)
            {
                if (__instance.cursorValid)
                {
                    while (__instance.buildPreviews.Count < 1)
                        __instance.buildPreviews.Add(new BuildPreview());
                    while (__instance.buildPreviews.Count > 1)
                        __instance.buildPreviews.RemoveAt(__instance.buildPreviews.Count - 1);
                    BuildPreview buildPreview = __instance.buildPreviews[0];
                    buildPreview.ResetAll();
                    buildPreview.item = __instance.handItem;
                    buildPreview.desc = __instance.handPrefabDesc;
                    buildPreview.needModel = false;
                    buildPreview.isConnNode = true;
                    buildPreview.genNearColliderArea2 = 20f;
                    buildPreview.inputObjId = __instance.castObjectId;
                    buildPreview.lpos = __instance.cursorTarget;
                    buildPreview.lpos2 = __instance.cursorTarget;
                }
                else
                    __instance.buildPreviews.Clear();
                __instance.actionBuild.model.cursorText = "选择起始位置".Translate();
                __instance.pathSuggest = 0;
            }
            else if (__instance.controller.cmd.stage == 1 && __instance.cursorValid)
            {
                __instance.waitForConfirm = true;
                int castObjectId = __instance.castObjectId;
                bool flag2 = __instance.startObjectId != 0 && !__instance.ObjectIsBelt(__instance.startObjectId);
                bool flag3 = castObjectId != 0 && !__instance.ObjectIsBelt(castObjectId);
                bool flag4 = __instance.startObjectId != 0 && __instance.ObjectIsAddonBuilding(__instance.startObjectId);
                bool flag5 = castObjectId != 0 && __instance.ObjectIsAddonBuilding(castObjectId);
                bool flag6 = __instance.startObjectId != 0 && __instance.ObjectIsBelt(__instance.startObjectId);
                bool flag7 = castObjectId != 0 && __instance.ObjectIsBelt(castObjectId);
                bool flag8 = __instance.startObjectId > 0;
                bool flag9 = castObjectId > 0;
                Pose[] poseArray1 = flag2 ? (flag4 ? __instance.GetLocalAddonPose(__instance.startObjectId) : __instance.GetLocalPorts(__instance.startObjectId)) : BuildTool.emptyPoseArr;
                Pose[] poseArray2 = flag3 ? (flag5 ? __instance.GetLocalAddonPose(castObjectId) : __instance.GetLocalPorts(castObjectId)) : BuildTool.emptyPoseArr;
                Vector3[] vector3Array1 = flag4 ? __instance.GetLocalAddonExt(__instance.startObjectId) : BuildTool.emptyExtArr;
                Vector3[] vector3Array2 = flag5 ? __instance.GetLocalAddonExt(castObjectId) : BuildTool.emptyExtArr;
                bool flag10 = poseArray1.Length != 0;
                bool flag11 = poseArray2.Length != 0;
                int nearestAddonAreaIdx = __instance.CalculateNearestAddonAreaIdx(castObjectId, __instance.castGroundPosSnapped);
                bool flag12 = __instance.startObjectId != 0 && __instance.startObjectId == castObjectId;
                int num1 = __instance.startObjectId != 0 || castObjectId != 0 ? (__instance.startObjectId != castObjectId ? 1 : 0) : 0;
                PrefabDesc prefabDesc1 = __instance.GetPrefabDesc(__instance.startObjectId);
                Pose objectPose1 = __instance.GetObjectPose(__instance.startObjectId);
                PrefabDesc prefabDesc2 = __instance.GetPrefabDesc(castObjectId);
                Pose objectPose2 = __instance.GetObjectPose(castObjectId);
                Vector3 begin = __instance.startTarget;
                Vector3 end = __instance.cursorTarget;
                Vector3 normalized = (__instance.cursorTarget - __instance.startTarget).normalized;
                int slot1 = -1;
                Vector3 b1 = Vector3.zero;
                Vector3 a1 = Vector3.zero;
                int slot2 = -1;
                Vector3 b2 = Vector3.zero;
                Vector3 a2 = Vector3.zero;
                Vector3 vector3_1;
                if (num1 != 0)
                {
                    int num2 = 0;
                    int num3 = 0;
                    if (flag10 | flag4)
                    {
                        float num4 = (float)((double)__instance.altitude * 1.33333325386047 + (double)__instance.planet.realRadius + 0.200000002980232) - __instance.startTarget.magnitude;
                        float num5 = -100000f;
                        for (int index = 0; index < poseArray1.Length; ++index)
                        {
                            Vector3 vector3_2 = objectPose1.position + objectPose1.rotation * poseArray1[index].position;
                            Vector3 vector3_3 = objectPose1.rotation * poseArray1[index].forward;
                            if (flag4)
                                vector3_3 = (double)Vector3.Dot(normalized, vector3_3) < 0.0 ? objectPose1.rotation * -poseArray1[index].forward : vector3_3;
                            Vector3 vector3_4 = flag4 ? vector3_2 + vector3_3 * Mathf.Max(__instance.GetGridWidth(vector3_2, vector3_3), Mathf.Abs(vector3Array1[index].z)) : vector3_2 + vector3_3 * 1.1f;
                            Vector3 dir = flag4 ? vector3_4 - vector3_2 : vector3_4 - objectPose1.position;
                            __instance.VectorProjectN(ref dir, flag4 ? vector3_2 : objectPose1.position);
                            float num6 = 0.04f - Mathf.Abs((float)(((double)num4 - (double)poseArray1[index].position.y) * 0.0399999991059303));
                            Vector3 lhs = __instance.cursorTarget - (flag4 ? vector3_2 : objectPose1.position);
                            Vector3 forward = objectPose1.forward;
                            Vector3 right = objectPose1.right;
                            Vector3 rhs1 = Maths.SphericalRotation(flag4 ? vector3_2 : objectPose1.position, 0.0f).Forward();
                            float f1 = Vector3.Dot(forward, rhs1);
                            float f2 = Vector3.Dot(right, rhs1);
                            float f3 = Mathf.Asin(flag4 ? vector3_2.normalized.y : objectPose1.position.normalized.y);
                            float num7 = (float)(((double)Mathf.Asin(__instance.cursorTarget.normalized.y) - (double)f3) * (flag4 ? (double)vector3_2.magnitude : (double)objectPose1.position.magnitude));
                            if (__instance.geodesic || (double)Mathf.Abs(f3) > 1.4835000038147 || poseArray1.Length <= 4)
                                f1 = f2 = 0.0f;
                            double num8 = (double)Mathf.Lerp(Vector3.Dot(lhs, forward), num7 * Mathf.Sign(f1), f1 * f1);
                            float num9 = Mathf.Lerp(Vector3.Dot(lhs, right), num7 * Mathf.Sign(f2), f2 * f2);
                            float num10 = prefabDesc1.buildCollider.ext.z + 1.25f;
                            float max1 = prefabDesc1.buildCollider.ext.x + 1.25f;
                            if (poseArray1.Length >= 12)
                            {
                                num10 += 1.25f;
                                max1 += 1.25f;
                            }
                            double min = -(double)num10;
                            double max2 = (double)num10;
                            double num11 = (double)Mathf.Clamp((float)num8, (float)min, (float)max2);
                            float num12 = Mathf.Clamp(num9, -max1, max1);
                            Vector3 vector3_5 = forward;
                            float num13 = Vector3.Dot(((float)num11 * vector3_5 + num12 * right).normalized, dir) + num6;
                            Vector3 rhs2 = Vector3.Cross(vector3_2, Vector3.up);
                            if ((double)rhs2.sqrMagnitude < 9.99999974737875E-05)
                                rhs2 = Vector3.zero;
                            else
                                rhs2.Normalize();
                            int num14 = (double)Mathf.Abs(Vector3.Dot(vector3_3, rhs2)) > 0.707000017166138 ? 1 : 2;
                            float num15 = num14 != __instance.pathAlternative ? num13 - 0.08f : num13 + 0.08f;
                            if (flag4)
                            {
                                if (index == __instance.startNearestAddonAreaIdx)
                                    num15 += 0.16f;
                                else
                                    num15 -= 0.16f;
                            }
                            if ((double)num15 > (double)num5)
                            {
                                num5 = num15;
                                slot1 = index;
                                b1 = vector3_2;
                                a1 = vector3_4;
                                num2 = num14;
                            }
                        }
                        if (slot1 >= 0)
                        {
                            begin = a1;
                            if (flag8 && prefabDesc1.isStation)
                            {
                                flag1 = true;
                                UIBeltBuildTip beltBuildTip = __instance.uiGame.beltBuildTip;
                                __instance.uiGame.OpenBeltBuildTip();
                                beltBuildTip.SetOutputEntity(__instance.startObjectId, slot1);
                                beltBuildTip.position = a1;
                                beltBuildTip.SetFilterToEntity();
                            }
                        }
                    }
                    if (flag11 | flag5)
                    {
                        float num16 = (float)((double)__instance.altitude * 1.33333325386047 + (double)__instance.planet.realRadius + 0.200000002980232) - __instance.cursorTarget.magnitude;
                        float num17 = -100000f;
                        for (int index = 0; index < poseArray2.Length; ++index)
                        {
                            Vector3 vector3_6 = objectPose2.position + objectPose2.rotation * poseArray2[index].position;
                            Vector3 vector3_7 = objectPose2.rotation * poseArray2[index].forward;
                            if (flag5)
                                vector3_7 = (double)Vector3.Dot(normalized, vector3_7) > 0.0 ? objectPose2.rotation * -poseArray2[index].forward : vector3_7;
                            Vector3 vector3_8 = flag5 ? vector3_6 + vector3_7 * Mathf.Max(__instance.GetGridWidth(vector3_6, vector3_7), Mathf.Abs(vector3Array2[index].z)) : vector3_6 + vector3_7 * 1.1f;
                            Vector3 dir = flag5 ? vector3_8 - vector3_6 : vector3_8 - objectPose2.position;
                            __instance.VectorProjectN(ref dir, flag5 ? vector3_6 : objectPose2.position);
                            float num18 = 0.04f - Mathf.Abs((float)(((double)num16 - (double)poseArray2[index].position.y) * 0.0399999991059303));
                            Vector3 lhs = __instance.startTarget - (flag5 ? vector3_6 : objectPose2.position);
                            Vector3 forward = objectPose2.forward;
                            Vector3 right = objectPose2.right;
                            Vector3 rhs3 = Maths.SphericalRotation(flag5 ? vector3_6 : objectPose2.position, 0.0f).Forward();
                            float f4 = Vector3.Dot(forward, rhs3);
                            float f5 = Vector3.Dot(right, rhs3);
                            float f6 = Mathf.Asin(flag5 ? vector3_6.normalized.y : objectPose2.position.normalized.y);
                            float num19 = (float)(((double)Mathf.Asin(__instance.startTarget.normalized.y) - (double)f6) * (flag5 ? (double)vector3_6.magnitude : (double)objectPose2.position.magnitude));
                            if (__instance.geodesic || (double)Mathf.Abs(f6) > 1.4835000038147 || poseArray2.Length <= 4)
                                f4 = f5 = 0.0f;
                            double num20 = (double)Mathf.Lerp(Vector3.Dot(lhs, forward), num19 * Mathf.Sign(f4), f4 * f4);
                            float num21 = Mathf.Lerp(Vector3.Dot(lhs, right), num19 * Mathf.Sign(f5), f5 * f5);
                            float num22 = prefabDesc2.buildCollider.ext.z + 1.25f;
                            float max3 = prefabDesc2.buildCollider.ext.x + 1.25f;
                            if (poseArray2.Length >= 12)
                            {
                                num22 += 1.25f;
                                max3 += 1.25f;
                            }
                            double min = -(double)num22;
                            double max4 = (double)num22;
                            double num23 = (double)Mathf.Clamp((float)num20, (float)min, (float)max4);
                            float num24 = Mathf.Clamp(num21, -max3, max3);
                            Vector3 vector3_9 = forward;
                            float num25 = Vector3.Dot(((float)num23 * vector3_9 + num24 * right).normalized, dir) + num18;
                            Vector3 rhs4 = Vector3.Cross(vector3_6, Vector3.up);
                            if ((double)rhs4.sqrMagnitude < 9.99999974737875E-05)
                                rhs4 = Vector3.zero;
                            else
                                rhs4.Normalize();
                            int num26 = (double)Mathf.Abs(Vector3.Dot(vector3_7, rhs4)) > 0.707000017166138 ? 2 : 1;
                            float num27 = num26 != __instance.pathAlternative ? num25 - 0.08f : num25 + 0.08f;
                            if (flag5)
                            {
                                if (index == nearestAddonAreaIdx)
                                    num27 += 0.16f;
                                else
                                    num27 -= 0.16f;
                            }
                            if ((double)num27 > (double)num17)
                            {
                                num17 = num27;
                                slot2 = index;
                                b2 = vector3_6;
                                a2 = vector3_8;
                                num3 = num26;
                            }
                        }
                        if (slot2 >= 0)
                            end = a2;
                    }
                    if (flag10 | flag4 && flag11 | flag5 && slot1 >= 0 && slot2 >= 0)
                    {
                        vector3_1 = b1 - b2;
                        double magnitude1 = (double)vector3_1.magnitude;
                        vector3_1 = a1 - a2;
                        float magnitude2 = vector3_1.magnitude;
                        int num28;
                        if (flag4)
                        {
                            vector3_1 = b1 - a1;
                            num28 = (double)vector3_1.magnitude > 1.10000002384186 ? 1 : 0;
                        }
                        else
                            num28 = 0;
                        bool flag13 = num28 != 0;
                        int num29;
                        if (flag5)
                        {
                            vector3_1 = b2 - a2;
                            num29 = (double)vector3_1.magnitude > 1.10000002384186 ? 1 : 0;
                        }
                        else
                            num29 = 0;
                        bool flag14 = num29 != 0;
                        if (magnitude1 < 1.70000004768372)
                        {
                            __instance.pathPointCount = 0;
                            __instance.pathPoints[__instance.pathPointCount] = b1;
                            ++__instance.pathPointCount;
                            if (flag13)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b1 + a1) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            if (flag14)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = b2;
                            ++__instance.pathPointCount;
                            goto label_141;
                        }
                        else if ((double)magnitude2 < 0.600000023841858)
                        {
                            __instance.pathPointCount = 0;
                            __instance.pathPoints[__instance.pathPointCount] = b1;
                            ++__instance.pathPointCount;
                            if (flag13)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b1 + a1) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = (a1 + a2) * 0.5f;
                            ++__instance.pathPointCount;
                            if (flag14)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = b2;
                            ++__instance.pathPointCount;
                            goto label_141;
                        }
                        else if ((double)magnitude2 < 1.04999995231628)
                        {
                            __instance.pathPointCount = 0;
                            __instance.pathPoints[__instance.pathPointCount] = b1;
                            ++__instance.pathPointCount;
                            if (flag13)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b1 + a1) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = Vector3.Lerp(a1, b1, 0.1f);
                            ++__instance.pathPointCount;
                            __instance.pathPoints[__instance.pathPointCount] = Vector3.Lerp(a2, b2, 0.1f);
                            ++__instance.pathPointCount;
                            if (flag14)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = b2;
                            ++__instance.pathPointCount;
                            goto label_141;
                        }
                        else if ((double)magnitude2 < 1.70000004768372)
                        {
                            __instance.pathPointCount = 0;
                            __instance.pathPoints[__instance.pathPointCount] = b1;
                            ++__instance.pathPointCount;
                            if (flag13)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b1 + a1) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = a1;
                            ++__instance.pathPointCount;
                            __instance.pathPoints[__instance.pathPointCount] = a2;
                            ++__instance.pathPointCount;
                            if (flag14)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = b2;
                            ++__instance.pathPointCount;
                            goto label_141;
                        }
                        else if ((double)magnitude2 < 2.79999995231628)
                        {
                            __instance.pathPointCount = 0;
                            __instance.pathPoints[__instance.pathPointCount] = b1;
                            ++__instance.pathPointCount;
                            if (flag13)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b1 + a1) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = a1;
                            ++__instance.pathPointCount;
                            __instance.pathPoints[__instance.pathPointCount] = (a1 + a2) * 0.5f;
                            ++__instance.pathPointCount;
                            __instance.pathPoints[__instance.pathPointCount] = a2;
                            ++__instance.pathPointCount;
                            if (flag14)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = b2;
                            ++__instance.pathPointCount;
                            goto label_141;
                        }
                        else if ((double)magnitude2 < 3.20000004768372)
                        {
                            __instance.pathPointCount = 0;
                            __instance.pathPoints[__instance.pathPointCount] = b1;
                            ++__instance.pathPointCount;
                            if (flag13)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b1 + a1) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = Vector3.LerpUnclamped(a1, b1, -0.15f);
                            ++__instance.pathPointCount;
                            __instance.pathPoints[__instance.pathPointCount] = (a1 + a2) * 0.5f;
                            ++__instance.pathPointCount;
                            __instance.pathPoints[__instance.pathPointCount] = Vector3.LerpUnclamped(a2, b2, -0.15f);
                            ++__instance.pathPointCount;
                            if (flag14)
                            {
                                __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                                ++__instance.pathPointCount;
                            }
                            __instance.pathPoints[__instance.pathPointCount] = b2;
                            ++__instance.pathPointCount;
                            goto label_141;
                        }
                    }
                    __instance.pathSuggest = num2 <= 0 || num3 != 0 && num2 != num3 ? (num3 <= 0 || num2 != 0 && num2 != num3 ? 0 : num3) : num2;
                }
                else if (flag12)
                {
                    __instance.pathPointCount = 1;
                    Vector3 vector3_10 = Vector3.zero;
                    if (poseArray1.Length == 1)
                        vector3_10 = poseArray1[0].position;
                    __instance.pathPoints[0] = objectPose1.position + objectPose1.rotation * vector3_10;
                    goto label_141;
                }
                if (VFInput._switchBeltsPath)
                {
                    if (!__instance.geodesic)
                    {
                        if (__instance.pathSuggest > 0)
                            __instance.geodesic = true;
                        if (__instance.pathAlternative == 1)
                            __instance.pathAlternative = 2;
                        else
                        {
                            __instance.pathAlternative = 1;
                            __instance.geodesic = true;
                        }
                    }
					else
					{
                        if (__instance.pathSuggest > 0)
                            __instance.geodesic = false;
                        if (__instance.pathAlternative == 1)
                            __instance.pathAlternative = 2;
                        else
                        {
                            __instance.pathAlternative = 1;
                            __instance.geodesic = false;
                        }
                    }
                }
                int path = __instance.pathSuggest > 0 ? __instance.pathSuggest : (__instance.pathAlternative > 0 ? __instance.pathAlternative : 1);
                __instance.pathPointCount = __instance.actionBuild.planetAux.SnapLineNonAlloc(begin, end, path, __instance.geodesic, !(flag10 | flag4), __instance.pathPoints);
                if (__instance.pathPointCount > 0)
                {
                    __instance.pathPoints[0] = begin;
                    __instance.pathPoints[__instance.pathPointCount - 1] = end;
                }
                if (slot1 >= 0)
                {
                    if (flag4)
                    {
                        vector3_1 = b1 - a1;
                        if ((double)vector3_1.magnitude > 1.70000004768372)
                        {
                            Array.Copy((Array)__instance.pathPoints, 0, (Array)__instance.pathPoints, 2, __instance.pathPointCount);
                            __instance.pathPoints[0] = b1;
                            __instance.pathPoints[1] = (b1 + a1) * 0.5f;
                            __instance.pathPointCount += 2;
                            goto label_130;
                        }
                    }
                    Array.Copy((Array)__instance.pathPoints, 0, (Array)__instance.pathPoints, 1, __instance.pathPointCount);
                    __instance.pathPoints[0] = b1;
                    ++__instance.pathPointCount;
                }
            label_130:
                if (slot2 >= 0)
                {
                    if (flag5)
                    {
                        vector3_1 = b2 - a2;
                        if ((double)vector3_1.magnitude > 1.70000004768372)
                        {
                            __instance.pathPoints[__instance.pathPointCount] = (b2 + a2) * 0.5f;
                            __instance.pathPoints[__instance.pathPointCount + 1] = b2;
                            __instance.pathPointCount += 2;
                            goto label_135;
                        }
                    }
                    __instance.pathPoints[__instance.pathPointCount] = b2;
                    ++__instance.pathPointCount;
                }
            label_135:
                for (int destinationIndex = 0; destinationIndex < __instance.pathPointCount - 1; ++destinationIndex)
                {
                    Vector3 pathPoint1 = __instance.pathPoints[destinationIndex];
                    Vector3 pathPoint2 = __instance.pathPoints[destinationIndex + 1];
                    Vector3 vector3_11 = pathPoint2 - pathPoint1;
                    if ((double)vector3_11.sqrMagnitude < 1e-6)
                    {
                        __instance.pathPoints[destinationIndex + 1] = __instance.pathPointCount != 2 ? (__instance.pathPointCount != 3 || destinationIndex != 1 || slot1 < 0 || slot2 >= 0 ? (__instance.pathPointCount != 3 || destinationIndex != 0 || slot2 < 0 || slot1 >= 0 ? (destinationIndex != 0 ? (destinationIndex != __instance.pathPointCount - 2 ? pathPoint1 + vector3_11 * 0.5f : pathPoint2) : pathPoint1) : pathPoint2) : pathPoint1) : pathPoint1 + vector3_11 * 0.5f;
                        Array.Copy((Array)__instance.pathPoints, destinationIndex + 1, (Array)__instance.pathPoints, destinationIndex, __instance.pathPointCount - destinationIndex - 1);
                        --__instance.pathPointCount;
                        --destinationIndex;
                    }
                }
            label_141:
                while (__instance.buildPreviews.Count < __instance.pathPointCount)
                    __instance.buildPreviews.Add(new BuildPreview());
                while (__instance.buildPreviews.Count > __instance.pathPointCount)
                    __instance.buildPreviews.RemoveAt(__instance.buildPreviews.Count - 1);
                int count = __instance.buildPreviews.Count;
                int index1 = count - 1;
                for (int index2 = 0; index2 < count; ++index2)
                {
                    BuildPreview buildPreview = __instance.buildPreviews[index2];
                    buildPreview.ResetAll();
                    buildPreview.item = __instance.handItem;
                    buildPreview.desc = __instance.handPrefabDesc;
                    buildPreview.lpos = __instance.pathPoints[index2];
                    buildPreview.lpos2 = buildPreview.lpos;
                    buildPreview.needModel = false;
                    buildPreview.isConnNode = true;
                    buildPreview.genNearColliderArea2 = index2 % 6 == 0 || index2 == __instance.pathPointCount - 1 ? 25f : 0.0f;
                }
                for (int index3 = 0; index3 < count - 1; ++index3)
                {
                    __instance.buildPreviews[index3].output = __instance.buildPreviews[index3 + 1];
                    __instance.buildPreviews[index3].outputObjId = 0;
                    __instance.buildPreviews[index3].outputFromSlot = 0;
                    __instance.buildPreviews[index3].outputToSlot = 1;
                    __instance.buildPreviews[index3].outputOffset = 0;
                }
                if (count > 0)
                {
                    bool flag15 = false;
                    bool flag16 = false;
                    bool isOutput;
                    int otherObjId;
                    int otherSlot;
                    if (slot1 >= 0 && !flag4)
                    {
                        __instance.factory.ReadObjectConn(__instance.startObjectId, slot1, out isOutput, out otherObjId, out otherSlot);
                        if (otherObjId != 0)
                            flag15 = true;
                    }
                    if (slot2 >= 0 && !flag5)
                    {
                        __instance.factory.ReadObjectConn(castObjectId, slot2, out isOutput, out otherObjId, out otherSlot);
                        if (otherObjId != 0)
                            flag16 = true;
                    }
                    if (slot1 >= 0)
                    {
                        PrefabDesc prefabDesc3 = __instance.GetPrefabDesc(__instance.startObjectId);
                        if (prefabDesc3 != null && prefabDesc3.isPiler)
                        {
                            __instance.factory.ReadObjectConn(__instance.startObjectId, 0, out isOutput, out otherObjId, out otherSlot);
                            if (isOutput && otherObjId != 0)
                                __instance.buildPreviews[0].condition = EBuildCondition.NeedConn;
                            __instance.factory.ReadObjectConn(__instance.startObjectId, 1, out isOutput, out otherObjId, out otherSlot);
                            if (isOutput && otherObjId != 0)
                                __instance.buildPreviews[0].condition = EBuildCondition.NeedConn;
                        }
                    }
                    if (slot2 >= 0)
                    {
                        PrefabDesc prefabDesc4 = __instance.GetPrefabDesc(castObjectId);
                        if (prefabDesc4 != null && prefabDesc4.isPiler)
                        {
                            __instance.factory.ReadObjectConn(castObjectId, 0, out isOutput, out otherObjId, out otherSlot);
                            if (!isOutput && otherObjId != 0)
                                __instance.buildPreviews[index1].condition = EBuildCondition.NeedConn;
                            __instance.factory.ReadObjectConn(castObjectId, 1, out isOutput, out otherObjId, out otherSlot);
                            if (!isOutput && otherObjId != 0)
                                __instance.buildPreviews[index1].condition = EBuildCondition.NeedConn;
                        }
                    }
                    if (__instance.startObjectId != 0 && slot1 >= 0)
                    {
                        if (flag4)
                        {
                            __instance.buildPreviews[0].addonbp = (BuildPreview)null;
                            __instance.buildPreviews[0].addonObjId = __instance.startObjectId;
                            __instance.buildPreviews[0].addonAreaIdx = slot1;
                        }
                        else
                        {
                            __instance.buildPreviews[0].input = (BuildPreview)null;
                            __instance.buildPreviews[0].inputObjId = __instance.startObjectId;
                            __instance.buildPreviews[0].inputFromSlot = slot1;
                            __instance.buildPreviews[0].inputToSlot = 1;
                            __instance.buildPreviews[0].inputOffset = 0;
                        }
                        if (__instance.buildPreviews.Count < 2)
                            __instance.buildPreviews[0].condition = EBuildCondition.TooShort;
                        if (flag15)
                        {
                            __instance.buildPreviews[0].condition = EBuildCondition.Occupied;
                            if (__instance.buildPreviews.Count >= 2)
                                __instance.buildPreviews[1].condition = EBuildCondition.Occupied;
                        }
                    }
                    if (castObjectId != 0 && slot2 >= 0)
                    {
                        if (flag5)
                        {
                            __instance.buildPreviews[index1].addonbp = (BuildPreview)null;
                            __instance.buildPreviews[index1].addonObjId = castObjectId;
                            __instance.buildPreviews[index1].addonAreaIdx = slot2;
                        }
                        else
                        {
                            __instance.buildPreviews[index1].output = (BuildPreview)null;
                            __instance.buildPreviews[index1].outputObjId = castObjectId;
                            __instance.buildPreviews[index1].outputFromSlot = 0;
                            __instance.buildPreviews[index1].outputToSlot = slot2;
                            __instance.buildPreviews[index1].outputOffset = 0;
                        }
                        if (__instance.buildPreviews.Count < 2)
                            __instance.buildPreviews[index1].condition = EBuildCondition.TooShort;
                        if (flag16)
                        {
                            __instance.buildPreviews[index1].condition = EBuildCondition.Occupied;
                            if (__instance.buildPreviews.Count >= 2)
                                __instance.buildPreviews[index1 - 1].condition = EBuildCondition.Occupied;
                        }
                    }
                    if (flag6)
                    {
                        int objectProtoId = __instance.GetObjectProtoId(__instance.startObjectId);
                        __instance.buildPreviews[0].coverObjId = __instance.startObjectId;
                        __instance.buildPreviews[0].willRemoveCover = objectProtoId != __instance.buildPreviews[0].item.ID;
                    }
                    if (flag7)
                    {
                        __instance.GetObjectProtoId(castObjectId);
                        __instance.buildPreviews[index1].coverObjId = castObjectId;
                        __instance.buildPreviews[index1].willRemoveCover = false;
                        if (count > 1)
                        {
                            if (flag9)
                                Array.Copy((Array)__instance.factory.entityConnPool, castObjectId * 16, (Array)__instance.tmp_conn, 0, 16);
                            else
                                Array.Copy((Array)__instance.factory.prebuildConnPool, -castObjectId * 16, (Array)__instance.tmp_conn, 0, 16);
                            if (__instance.tmp_conn[1] == 0)
                                __instance.buildPreviews[index1 - 1].outputToSlot = 1;
                            else if (__instance.tmp_conn[2] == 0)
                                __instance.buildPreviews[index1 - 1].outputToSlot = 2;
                            else if (__instance.tmp_conn[3] == 0)
                                __instance.buildPreviews[index1 - 1].outputToSlot = 3;
                            else
                                __instance.buildPreviews[index1 - 1].outputToSlot = 14;
                        }
                    }
                    if (flag6 & flag7 && count <= 2)
                    {
                        __instance.buildPreviews[0].willRemoveCover = true;
                        __instance.buildPreviews[index1].willRemoveCover = true;
                    }
                    if (castObjectId != 0 && !flag7 && (__instance.ObjectIsAddonBuilding(castObjectId) ? __instance.GetLocalAddonPose(castObjectId) : __instance.GetLocalPorts(castObjectId)).Length == 0)
                    {
                        __instance.buildPreviews[index1].condition = EBuildCondition.BeltCannotConnectToBuildingWithInserterTip;
                        if (__instance.buildPreviews.Count > 1)
                            __instance.buildPreviews[index1 - 1].condition = EBuildCondition.BeltCannotConnectToBuildingWithInserterTip;
                        if (__instance.buildPreviews.Count > 2)
                            __instance.buildPreviews[index1 - 2].condition = EBuildCondition.BeltCannotConnectToBuildingWithInserterTip;
                    }
                }
            }
            if (__instance.controller.cmd.stage == 0)
            {
                __instance.startObjectId = 0;
                __instance.startNearestAddonAreaIdx = 0;
                __instance.startTarget = Vector3.zero;
                __instance.pathPointCount = 0;
            }
            if (flag1)
                return false;
            __instance.uiGame.beltBuildTip.SetOutputEntity(0, -1);
            __instance.uiGame.CloseBeltBuildTip();
            return false;
        }

    }
}
