using BepInEx;
using HarmonyLib;
using Potassium.Threading;

namespace Potassium
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("Project Arrhythmia.exe")]
    public class Potassium : BaseUnityPlugin
    {
        public const string
            Name = "Potassium",
            Guid = "org.reimnop.potassium",
            Version = "1.4.0";

        private void Awake()
        {
            ThreadManager.InitWorkers(8);   

            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll();
        }
    }
}
