using BepInEx;
using HarmonyLib;

namespace Potassium
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("Project Arrhythmia.exe")]
    public class Potassium : BaseUnityPlugin
    {
        public const string
            Name = "Potassium",
            Guid = "org.reimnop.potassium",
            Version = "1.5.0";

        private void Awake()
        {
            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll();
        }
    }
}
