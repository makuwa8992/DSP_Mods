using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongerBelts
{
	class Translate
	{
		public string distance_setting;
		public string distance_units;
		public string[] distance_unitsStrings = {"米", "格(赤道)" };
		public string pathMode;
		public string[] pathModeStrings = { "原版升降逻辑(水平移动一格后每节升降0.5层至目标高度)", "\"均匀\"升降(端点不水平，常规游戏需先在两端拉好水平带\n建议配合如建筑铺设无条件等放宽传送带铺设条件的功能使用)" };
		public string WarningNotice;
		public string unlimit_distance_instruction;
		public string unlimit_distance_setting;
		static public Translate NewTexture(Language language)
		{
			Translate texture = new Translate();
			if(language == Language.zhCN)
			{
				texture.distance_setting = "最大间距设置";
				texture.distance_units = "单位:";
				texture.distance_unitsStrings[0] = "米";
				texture.distance_unitsStrings[1] = "格(赤道)";
				texture.pathMode = "传送带路径";
				texture.pathModeStrings[0] = "原版升降逻辑(水平移动一格后每节升降0.5层至目标高度)";
				texture.pathModeStrings[1] = "垂直等距分割(端点不水平，常规游戏需先在两端拉好水平带\n建议配合如建筑铺设无条件等放宽传送带铺设条件的功能使用)";
				texture.WarningNotice = "下列功能慎用!";
				texture.unlimit_distance_instruction = "勾选以启用弱约束间距输入框(常规游戏中会出现传送带过短或过长等错误\n即使使用无条件铺设也可能触发特殊bug,造出的蓝图也未必能用)";
				texture.unlimit_distance_setting = "弱约束间距输入框(配合铺设无条件作弊、测试用):";
			}
			else if(language == Language.enUS)
			{
				texture.distance_setting = "Maximum Spacing(unit: meters, not grid)";
				texture.distance_units = "Units:";
				texture.distance_unitsStrings[0] = "meters";
				texture.distance_unitsStrings[1] = "Grid(At equator)";
				texture.pathMode = "Belt Path Mode";
				texture.pathModeStrings[0] = "Original elevating logic(moving horizontally one grid and changing\nelevation by 0.5 units per segment to reach the target height)";
				texture.pathModeStrings[1] = "Vertical equidistant(horizontal conveyor belts at both ends \n or other auxiliary mods are required)";
				texture.WarningNotice = "Exercise caution when modifying the following settings!";
				texture.unlimit_distance_instruction = "Check to enable the following input box(certain numbers can lead\nto errors such as belts being too short or too long during vanilla game.)";
				texture.unlimit_distance_setting = "Maximum Spacing(less constraints)";
			}
			return texture;
		}
	}
}
