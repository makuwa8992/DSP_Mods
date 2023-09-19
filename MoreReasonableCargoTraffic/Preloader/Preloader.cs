using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Rocks;


namespace MoreReasonableCargoTraffic
{
    public class Preloader
    {
        public static ManualLogSource logSource;

        public static void Initialize() => Preloader.logSource = Logger.CreateLogSource("DSPOptimizations Preloader");

        public static IEnumerable<string> TargetDLLs { get; } = (IEnumerable<string>)new string[1]
        {
      "Assembly-CSharp.dll"
        };

        private static TypeDefinition GetType(AssemblyDefinition assembly, string type)
        {
            TypeDefinition type1 = assembly.MainModule.GetType(type);
            if (type1 != null)
                return type1;
            Preloader.logSource.LogError((object)("Preloader patch failed: unable to get type " + type));
            return type1;
        }
        public static void Patch(AssemblyDefinition assembly)
        {
            TypeSystem typeSystem = assembly.MainModule.TypeSystem;
            TypeDefinition type1 = Preloader.GetType(assembly, "CargoPath");
            Preloader.logSource.LogInfo((object)"try to run MoreReasonableCargoTraffic preloader patch");
            if (type1 != null)
            {
                type1.Fields.Add(new FieldDefinition("lastUpdate", FieldAttributes.Public,typeSystem.Boolean));
                TypeDefinition type2 = type1;
                if (type2 != null)
                {
                    type2.Fields.Add(new FieldDefinition("outputChunk", FieldAttributes.Public, typeSystem.Int32));
                    Preloader.logSource.LogInfo((object)"Successfully added lastUpdate");
                }
            }
            Preloader.logSource.LogInfo((object)"Successfully ran MoreReasonableCargoTraffic preloader patch");
        }
    }
}
