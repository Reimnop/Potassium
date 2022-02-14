using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Potassium
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("Project Arrhythmia.exe")]
    public class Potassium : BaseUnityPlugin
    {
        public static ManualLogSource Logger;
        
        public const string
            Name = "Potassium",
            Guid = "org.reimnop.potassium",
            Version = "1.5.1";

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("Potassium");
            
            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll();
        }
    }
}
