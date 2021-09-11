using HarmonyLib;
using System.Collections.Generic;

namespace Potassium
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatcher
    {
        [HarmonyPatch("PlayLevel")]
        [HarmonyPostfix]
        public static void PlayLevelPostfix()
        {
            List<DataManager.GameData.BeatmapObject> beatmapObjects = DataManager.inst.gameData.beatmapObjects;

            // We add all the objects to the dictionary for fast lookup later on.
            ObjectManagerPatcher.BeatmapObjectsLookup = new Dictionary<string, DataManager.GameData.BeatmapObject>();
            foreach (DataManager.GameData.BeatmapObject beatmapObject in beatmapObjects)
            {
                // No TryAdd :(
                if (!ObjectManagerPatcher.BeatmapObjectsLookup.ContainsKey(beatmapObject.id))
                {
                    ObjectManagerPatcher.BeatmapObjectsLookup.Add(beatmapObject.id, beatmapObject);
                }
            }

            ObjectManagerPatcher.InitLevel();
        }
    }
}
