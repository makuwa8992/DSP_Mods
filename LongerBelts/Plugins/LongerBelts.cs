using System;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LongerBelts
{
    [BepInPlugin("shisang_LongerBelts", "LongerBelts", "1.5.0")]

    public class LongerBelts : BaseUnityPlugin
    {
        private bool DisplayingWindow = false;
        // 启动按键
        private ConfigEntry<KeyboardShortcut> SettingWindow{ get; set; }
        private ConfigEntry<float> WindowScale { get; set; }
        private ConfigEntry<bool> LongerOnGrid { get; set; }
        private ConfigEntry<int> IsometricSegmentation { get; set; }
        private ConfigEntry<float> CurrentDistance { get; set; }
        private ConfigEntry<float> LongitudeDistance { get; set; }
        private ConfigEntry<float> LatitudeDistance { get; set; }
        private Rect windowRect = new Rect(200, 200, 600, 400);
        static public bool shortestUnlimit = false;
        static public float currentDistance = 1.9f;
        static public float longitudeDistance = 1.75f;
        static public float latitudeDistance = 1.5f;
        private readonly float minimumGridDistance = 1.0f;
        private readonly float maxmumGridDistance = 3.0f;
        static public float minimum_distance = 0.400001f;
        private readonly float maxmum_distance = 2.302172f;
        static public int isometricSegmentation = 0;
        static public bool longerOnGrid = false;
        private int distance_units = 0;
        private Translate UItexture;
        static public float test;
        private KeyboardShortcut enlargeWindow = new KeyboardShortcut(KeyCode.LeftControl, KeyCode.UpArrow);
        private KeyboardShortcut shortenWindow = new KeyboardShortcut(KeyCode.LeftControl, KeyCode.DownArrow);
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(LongerBelts));
            Debug.Log("Add LongerBelts");
        }
        void Start()
        {
            SettingWindow = Config.Bind("打开窗口快捷键/HotKey", "Key", new KeyboardShortcut(KeyCode.Alpha3, KeyCode.R));
            WindowScale = Config.Bind("默认窗口尺寸/WindowScale", "WindowScale", 200f);
            LongerOnGrid = Config.Bind("更改经纬线模式默认跨度/Change default span in gridline mode", "Enable", false);
            IsometricSegmentation = Config.Bind("斜坡带模式/Slope Path Mode", "Enable", 0, "0为原始模式，1为阿基米德螺线模式/A value of 0 indicates the use of the default mode, whereas a value of 1 signifies the adoption of the Archimedean spiral pattern.");
            CurrentDistance = Config.Bind("最远距离/Maximum Spacing", "Distance", 1.9f);
            LongitudeDistance = Config.Bind("沿纬线最远距离/Maximum Longitude Distance", "Distance", 1.75f);
            LatitudeDistance = Config.Bind("沿经线最远距离/Maximum Latitude Distance", "Distance", 1.5f);
            windowRect.width = 3 * WindowScale.Value;
            windowRect.height = 2 * WindowScale.Value;
            longerOnGrid = LongerOnGrid.Value;
            isometricSegmentation = IsometricSegmentation.Value;
            currentDistance = CurrentDistance.Value;
            longitudeDistance = LongitudeDistance.Value;
            latitudeDistance = LatitudeDistance.Value;
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
            if (DisplayingWindow)
            {
                GUI.backgroundColor = Color.gray;
                UItexture = Translate.NewTexture();
                windowRect = GUI.Window(20231008, windowRect, SetLongerBelts, "LongerBelts"+ UItexture.resize_window);
            }
        }

        public void SetLongerBelts(int winId)
        {
            GUI.skin.label.fontSize = (int)(WindowScale.Value / 12);
            GUIStyle customToggleStyle = new GUIStyle(GUI.skin.toggle);
            float ComponentHeight = 0.1f * WindowScale.Value;
            customToggleStyle.fontSize = (int)(WindowScale.Value / 12);
            customToggleStyle.fixedHeight = ComponentHeight;
            GUIStyle customSliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            customSliderStyle.fixedHeight = 0.05f * WindowScale.Value;
            customSliderStyle.stretchHeight = false;
            customSliderStyle.padding.top = (int)((customSliderStyle.fixedHeight - ComponentHeight))/2;
            GUIStyle thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumbStyle.fixedHeight = ComponentHeight;
            thumbStyle.fixedWidth = 0.05f * WindowScale.Value;
            GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.fontSize = (int)(WindowScale.Value / 12);
            GUI.DragWindow(new Rect(0, 0, windowRect.width, WindowScale.Value / 12));
            GUILayout.BeginVertical();
            longerOnGrid = GUILayout.Toggle(longerOnGrid, UItexture.ifLongerOnGrid, customToggleStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(UItexture.longitudeDistance, GUILayout.Width(0.7f * WindowScale.Value));
            GUILayout.BeginVertical();
            GUILayout.Space(ComponentHeight/2);
            longitudeDistance = GUILayout.HorizontalSlider(longitudeDistance, minimumGridDistance, maxmumGridDistance, customSliderStyle, thumbStyle, GUILayout.Width(0.4f * WindowScale.Value));
            GUILayout.EndVertical();
            string input_distance = GUILayout.TextField(longitudeDistance.ToString("0.000"), textFieldStyle, GUILayout.Width(0.25f * WindowScale.Value));
            if (float.TryParse(input_distance, out float temp_distance))
            {
                if (temp_distance < maxmumGridDistance && temp_distance > minimumGridDistance)
                {
                    longitudeDistance = temp_distance;
                }
            }
            GUILayout.Label(UItexture.latitudeDistance, GUILayout.Width(0.7f * WindowScale.Value));
            GUILayout.BeginVertical();
            GUILayout.Space(ComponentHeight / 2);
            latitudeDistance = GUILayout.HorizontalSlider(latitudeDistance, minimumGridDistance, maxmumGridDistance, customSliderStyle, thumbStyle, GUILayout.Width(0.4f * WindowScale.Value));
            GUILayout.EndVertical();
            input_distance = GUILayout.TextField(latitudeDistance.ToString("0.000"), textFieldStyle, GUILayout.Width(0.25f * WindowScale.Value));
            if (float.TryParse(input_distance, out temp_distance))
            {
                if (temp_distance < maxmumGridDistance && temp_distance > minimumGridDistance)
                {
                    latitudeDistance = temp_distance;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(UItexture.distance_setting, GUILayout.Width(1.5f * WindowScale.Value));
            GUILayout.BeginVertical();
            GUILayout.Space(ComponentHeight / 2);
            currentDistance = GUILayout.HorizontalSlider(currentDistance, minimum_distance, maxmum_distance, customSliderStyle, thumbStyle, GUILayout.Width(0.5f * WindowScale.Value));
            GUILayout.EndVertical(); 
            input_distance = GUILayout.TextField((distance_units == 1 ? currentDistance / 1.256637f : currentDistance).ToString("0.000000"), textFieldStyle, GUILayout.Width(0.7f * WindowScale.Value));
			if (float.TryParse(input_distance,out temp_distance))
			{
                if (distance_units == 1) temp_distance *= 1.256637f;
                if (temp_distance < maxmum_distance && temp_distance > minimum_distance)
			    {
                    currentDistance = temp_distance;
                }
			}
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(UItexture.distance_units, GUILayout.Width(0.25f * WindowScale.Value));
            distance_units = GUILayout.SelectionGrid(distance_units, UItexture.distance_unitsStrings, 2, customToggleStyle, GUILayout.Width(1.5f * WindowScale.Value));
            GUILayout.EndHorizontal();
            GUILayout.Label(UItexture.pathMode);
            isometricSegmentation = GUILayout.SelectionGrid(isometricSegmentation, UItexture.pathModeStrings, 1, customToggleStyle);
            GUILayout.Label("");
            GUILayout.Label(UItexture.WarningNotice);
            shortestUnlimit = GUILayout.Toggle(shortestUnlimit, UItexture.unlimit_distance_instruction, customToggleStyle);
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(UItexture.unlimit_distance_setting, GUILayout.Width(2f * WindowScale.Value));
            string unlimited_input_distance = GUILayout.TextField((distance_units == 1 ? currentDistance / 1.256637f : currentDistance).ToString("0.000000"), textFieldStyle, GUILayout.Width(0.7f * WindowScale.Value));
            if (float.TryParse(unlimited_input_distance, out temp_distance) && shortestUnlimit)
            {
                if (distance_units == 1) temp_distance *= 1.256637f;
                if (temp_distance < 0.001f) temp_distance = 0.001f;
                if(temp_distance > 999f) temp_distance = 999f;
                currentDistance = temp_distance;
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


        public static int LongerSnapLineNonAlloc(PlanetGrid planetGrid , Vector3 begin, Vector3 end, int path, Vector3[] snaps)
        {
			if (LongerBelts.longerOnGrid)
			{
                int num1 = snaps.Length - 10;//snaps.Length在某个地方初始化的时候是160,不知道这里过程中有没有改,即snaps最多生成160个,num1=150
                if (num1 <= 0)
                    return 0;
                begin.Normalize();
                end.Normalize();
                float f1 = Mathf.Asin(begin.y);//起始点纬度90°S~90°N对应[-π/2,π/2]
                float num2 = Mathf.Atan2(begin.x, -begin.z);//起始点经度arctan(-x/z),范围[-π,π]
                float num3 = Mathf.Asin(end.y);//终止点纬度
                float num4 = Mathf.Atan2(end.x, -end.z);//终止点经度
                float f2 = Mathf.Repeat(num4 - num2, 6.283185f);//f2 = num4-num2±2kπ,f2∈[0,2π]，始末端点的经度跨度
                float heigherLatitude = Mathf.Max(Mathf.Abs(f1), Mathf.Abs(num3));
                Vector3 beginNormalized = begin.normalized;
                Vector3 endNormalized = end.normalized;
                int count = 0;
                snaps[count++] = beginNormalized;
                if ((double)f2 > 3.14159274101257)
                    f2 -= 6.283185f;//f2 = num4-num2±2kπ,f2∈[-π,π]
                if (path == 1)//先沿纬线走,再沿经线走
                {
                    float longitudeSegmentCount = (float)PlanetGrid.DetermineLongitudeSegmentCount(Mathf.FloorToInt(Mathf.Max(0.0f, Mathf.Abs(f1 / 6.283185f * (float)planetGrid.segment) - 0.1f)), planetGrid.segment);//起始点纬度的经线分割数/5
                    if ((Mathf.Abs(f2) - 1.57079637f) * longitudeSegmentCount > (1.57079637f - heigherLatitude) * 200)
				    {
                        if (f2 > 0)
					    {
                            f2 -= 3.141593f;//f2∈[-π,π]
                        }
					    else
					    {
                            f2 += 3.141593f;//f2∈[-π,π]
                        }
                        num3 = (double)num3 < 0.0 ? -3.141593f - num3 : 3.141593f - num3;//num3∈[-π,-π/2]∪[π/2,π]意义:2*π/2-num3,意思就是对着90°的极点翻转到另一端
                    }
                    float longitude1 = num2 + f2;//目标点的假想经度
                    float f3 = num3 - f1;//目标点纬度跨度
                    int longitudeSnaps = (int)(Mathf.Abs(f2) / 1.2566370614357f * longitudeSegmentCount / LongerBelts.latitudeDistance) + 1;//沿纬线总步数
                    if(Mathf.Abs(f2) < 1e-4)
					{
                        longitudeSnaps = 0;
					}
                    LongerBelts.test = longitudeSegmentCount;
                    int latitudeSnaps = (int)(Mathf.Abs(f3) / 0.0062831853f / LongerBelts.longitudeDistance) + 1;//沿经线总步数
                    float num8 = Mathf.Cos(f1);//起始点纬线的半径
                    for (int index = 1; index <= longitudeSnaps; ++index)//沿纬线走,根据经度生成坐标
                    {
                        float t = f2 * (float)index / (float)longitudeSnaps;
                        snaps[count++] = new Vector3(num8 * Mathf.Sin(num2 + t), Mathf.Sin(f1), -num8 * Mathf.Cos(num2 + t));
                    }
                    Vector3 midNormalized = new Vector3(num8 * Mathf.Sin(longitude1), Mathf.Sin(f1), -num8 * Mathf.Cos(longitude1));//中间点
                    if((endNormalized- midNormalized).magnitude < 0.00251f)//跨度小于0.4格
				    {
                        return count;
                    }
                    for (int index = 1; index <= latitudeSnaps; ++index)//沿经线走,直接插值
                    {
                        float t = (float)index / (float)latitudeSnaps;
                        snaps[count++] = Vector3.Slerp(midNormalized, endNormalized, t).normalized;
                    }
                    return count;
                }
                else//先沿经线走,再沿纬线走
                {
                    float longitudeSegmentCount = (float)PlanetGrid.DetermineLongitudeSegmentCount(Mathf.FloorToInt(Mathf.Max(0.0f, Mathf.Abs(num3 / 6.283185f * (float)planetGrid.segment) - 0.1f)), planetGrid.segment);//终止点维度的经线分割数/5
                    if ((Mathf.Abs(f2) - 1.57079637f) * longitudeSegmentCount > (1.57079637f - heigherLatitude) * 200)
                    {
                        if (f2 > 0)
                        {
                            f2 -= 3.141593f;//f2∈[2-π,2]
                        }
                        else
                        {
                            f2 += 3.141593f;//f2∈[2-π,2]
                        }
                        num3 = (double)num3 < 0.0 ? -3.141593f - num3 : 3.141593f - num3;//num3∈[-π,π]意义:2*π/2-num3,意思就是对着90°的极点翻转到另一端
                    }
                    float f3 = num3 - f1;//目标点纬度跨度
                    int longitudeSnaps = (int)(Mathf.Abs(f2) / 1.2566370614357f * longitudeSegmentCount / LongerBelts.latitudeDistance) + 1;//沿纬线总步数
                    int latitudeSnaps = (int)(Mathf.Abs(f3) / 0.0062831853f / LongerBelts.longitudeDistance) + 1;//沿经线总步数
                    if (Mathf.Abs(f3) < 1e-4)
                    {
                        latitudeSnaps = 0;
                    }
                    float num8 = Mathf.Cos(num3);//目标点纬线的半径(这里实际上是负的)
                    Vector3 midNormalized = new Vector3(num8 * Mathf.Sin(num2), Mathf.Sin(num3), -num8 * Mathf.Cos(num2));//中间点
                    for (int index = 1; index <= latitudeSnaps; ++index)//沿经线走,直接插值
                    {
                        float t = (float)index / (float)latitudeSnaps;
                        snaps[count++] = Vector3.Slerp(beginNormalized, midNormalized, t).normalized;
                    }
                    if (Mathf.Abs(f2) / 1.2566370614357f * longitudeSegmentCount < 0.4f)//跨度小于0.4格
                    {
                        return count;
                    }
                    for (int index = 1; index <= longitudeSnaps; ++index)//沿纬线走,根据经度生成坐标
                    {
                        float t = num2 + ((float)index / (float)longitudeSnaps) * f2;
                        snaps[count++] = new Vector3(num8 * Mathf.Sin(t), Mathf.Sin(num3), -num8 * Mathf.Cos(t));//num3可能>90°,此时num8是负数,sin(num3) = sin(180°-num3)
                    }
                    return count;
                }
			}
			else
			{
                return planetGrid.SnapLineNonAlloc(begin, end, path, snaps);
			}
        }
        public static bool LongerGeodesic(Vector3 begin, Vector3 end, ref int counts, Vector3[] snaps)
        {
            int maxSnapsLength = 150;
            float max_radius = begin.magnitude > end.magnitude ? begin.magnitude : end.magnitude;
            Vector3 beginNormalized = begin.normalized;
            Vector3 endNormalized = end.normalized;
            float nodes_counts;
            float geodesic_distance = Mathf.Acos(Mathf.Clamp(Vector3.Dot(beginNormalized, endNormalized), -1, 1)) * max_radius;
            int num10;
            if (LongerBelts.isometricSegmentation == 1)//阿基米德螺线模式
            {
                float beginMagnitude = begin.magnitude;
                float endMagnitude = end.magnitude;
                float delta_height = Mathf.Abs(endMagnitude - beginMagnitude);//起止点高度差
                nodes_counts = Mathf.Sqrt(geodesic_distance * geodesic_distance + delta_height * delta_height) / LongerBelts.currentDistance;
                if (!LongerBelts.shortestUnlimit && Mathf.Sqrt(geodesic_distance * geodesic_distance + delta_height * delta_height) / ((int)nodes_counts + 1) < LongerBelts.minimum_distance)//实际建造时传送带距离过短判定是<=0.4m
                {
                    nodes_counts = Mathf.Sqrt(geodesic_distance * geodesic_distance + delta_height * delta_height) / LongerBelts.minimum_distance - 1;
                }
                num10 = nodes_counts > 0.1 ? (int)nodes_counts + 1 : 0;
                if (num10 == 0)
                {
                    snaps[counts++] = beginNormalized;
                    if ((double)Mathf.Abs(beginMagnitude - endMagnitude) > 1.0 / 1000.0)
                    {
                        snaps[counts++] = snaps[0] * endMagnitude;
                        return false;
                    }
                }
                else
                {
                    for (int index = 0; index <= num10 && counts < maxSnapsLength; ++index)
                    {
                        float t = (float)index / (float)num10;
                        snaps[counts++] = Vector3.Slerp(beginNormalized, endNormalized, t).normalized;
                    }
                }
                for (int index = 0; index < counts; ++index)
                {
                    snaps[index] *= beginMagnitude * (float)(counts - 1 - index) / (float)(counts - 1) + endMagnitude * (float)index / (float)(counts - 1);
                }
                return false;
            }
            nodes_counts = geodesic_distance / LongerBelts.currentDistance;//类似原版逻辑(只是间隔变长了)
            if (!LongerBelts.shortestUnlimit && Mathf.Acos(Vector3.Dot(beginNormalized, endNormalized)) * (begin.magnitude < max_radius ? begin.magnitude : end.magnitude) / ((int)nodes_counts + 1) < LongerBelts.minimum_distance)//实际建造时传送带距离过短判定是<=0.4m
            {
                nodes_counts = (Mathf.Acos(Vector3.Dot(beginNormalized, endNormalized)) * (begin.magnitude < max_radius ? begin.magnitude : end.magnitude) / LongerBelts.minimum_distance) - 1;
            }
            num10 = nodes_counts > 0.1 ? (int)nodes_counts + 1 : 0;
            if (num10 == 0)
            {
                snaps[counts++] = beginNormalized;
            }
            else
            {
                for (int index = 0; index <= num10 && counts < maxSnapsLength; ++index)
                {
                    float t = (float)index / (float)num10;
                    snaps[counts++] = Vector3.Slerp(beginNormalized, endNormalized, t).normalized;
                }
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetAuxData), "SnapLineNonAlloc")]
        private static IEnumerable<CodeInstruction> PlanetAuxData_SnapLineNonAlloc_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
		{
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetGrid), nameof(PlanetGrid.SnapLineNonAlloc))));
            matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(LongerBelts), nameof(LongerBelts.LongerSnapLineNonAlloc)));
            var snaps = matcher.InstructionAt(-2).operand;
            var jmpBeltGeneration = matcher.InstructionAt(1).operand;
            var beginLF = matcher.InstructionAt(4).operand;
            var endLF = matcher.InstructionAt(7).operand;
            matcher.Advance(5);
            matcher.CreateLabelAt(matcher.Pos, out var jmpShorterPath);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_3),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Bne_Un_S,jmpShorterPath),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldloca_S,0),
                new CodeInstruction(OpCodes.Ldarg_S, snaps),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LongerBelts), nameof(LongerBelts.LongerGeodesic))),
                new CodeInstruction(OpCodes.Brtrue_S, jmpBeltGeneration),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ret)
                );
            return matcher.InstructionEnumeration();
        }

		public static void SwitchBeltsPath(BuildTool_Path path)
        {
            if (path.pathSuggest > 0)
                path.geodesic = false;
			if (path.pathAlternative == 1)
                path.pathAlternative = 2;
			else
			{
                path.pathAlternative = 1;
                path.geodesic = false;
			}
        }

        public static void SlantVerticalBelts(Vector3 begin,ref Vector3 end)
		{
            if ((begin.normalized - end.normalized).magnitude < 0.0001f)
            {
                // 新模式垂直带水平距离过近调整,保证垂直带坡度<1000,使垂直传送带美观
                Vector3 littleOffset = new Vector3(0, 0.001f, 0);
                if (Mathf.Abs(Vector3.Dot(begin.normalized, littleOffset.normalized)) > 0.99f)//如果接近极点就改对赤道上一点叉乘
                {
                    littleOffset = new Vector3(0.001f, 0, 0);
                }
                littleOffset = Vector3.Cross(end - begin, littleOffset);//横向偏置=纵向跨度叉乘一个模长为0.001的向量(通常指向北极),相当于保证坡度不大于1000
                end += littleOffset;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        private static IEnumerable<CodeInstruction> BuildTool_Path_DeterminePreviews_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
		{
            var matcher = new CodeMatcher(instructions, generator);

            // 模式切换
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Stfld,
                    AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.geodesic)))
            );
            matcher.SetAndAdvance(OpCodes.Ldarg_0, null);
            matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(LongerBelts), nameof(LongerBelts.SwitchBeltsPath)));
            matcher.SetAndAdvance(OpCodes.Nop,null);

            //垂直带小倾斜以保证美观
            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.SnapLineNonAlloc))));
            //Debug.Log("Position:"+matcher.Pos);
            var begin = matcher.InstructionAt(-12).operand;
            var end = matcher.InstructionAt(-11).operand;
            matcher.Advance(-16)
                .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, begin),
                new CodeInstruction(OpCodes.Ldloca_S, end),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LongerBelts), nameof(LongerBelts.SlantVerticalBelts))));
            //Debug.Log("Begin:"+begin);
            //Debug.Log("END:"+end);

            return matcher.InstructionEnumeration();
        }

    }
}
