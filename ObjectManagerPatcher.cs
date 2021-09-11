using HarmonyLib;
using LeaderAnimator;
using Potassium.Threading;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Potassium
{
    [HarmonyPatch(typeof(ObjectManager))]
    public class ObjectManagerPatcher
    {
        private static Dictionary<string, Easing> stringToEasings = new Dictionary<string, Easing>()
        {
            { "Linear", Easing.Linear },
            { "Instant", Easing.Instant },
            { "InSine", Easing.InSine },
            { "OutSine", Easing.OutSine },
            { "InOutSine", Easing.InOutSine },
            { "InElastic", Easing.InElastic },
            { "OutElastic", Easing.OutElastic },
            { "InOutElastic", Easing.InOutElastic },
            { "InBack", Easing.InBack },
            { "OutBack", Easing.OutBack },
            { "InOutBack", Easing.InOutBack },
            { "InBounce", Easing.InBounce },
            { "OutBounce", Easing.OutBounce },
            { "InOutBounce", Easing.InOutBounce },
            { "InQuad", Easing.InQuad },
            { "OutQuad", Easing.OutQuad },
            { "InOutQuad", Easing.InOutQuad },
            { "InCirc", Easing.InCirc },
            { "OutCirc", Easing.OutCirc },
            { "InOutCirc", Easing.InOutCirc },
            { "InExpo", Easing.InExpo },
            { "OutExpo", Easing.OutExpo },
            { "InOutExpo", Easing.InOutExpo }
        };

        public static Dictionary<string, DataManager.GameData.BeatmapObject> BeatmapObjectsLookup;

        private static Dictionary<string, SequencesPair> sequencesLookup;

        private static List<LevelObjectRef> levelObjects;
        private static List<LevelObjectRef> aliveObjects;

        private static List<LevelObjectAction> objectActions;

        private static int index;
        private static float lastAudioTime;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool UpdatePrefix()
        {
            // Return true = don't skip original method
            // Since Editor is not implemented, we skip this pipeline and use the old pipeline instead.
            if (EditorManager.inst != null && EditorManager.inst.isEditing)
                return true;

            if (DataManager.inst.gameData.beatmapObjects.Count == 0)
                return false;

            float time = AudioManager.inst.CurrentAudioSource.time;

            float timeDelta = time - lastAudioTime;

            // We have saved all the objects into one array called ObjectActions in order to always have in order spawn and despawn.
            if (timeDelta >= 0f) // If time is going in forward direction
            {
                while (index < objectActions.Count && objectActions[index].Time < time)
                {
                    LevelObjectAction objectAction = objectActions[index];
                    if (objectAction.ActionType == ObjectActionType.Spawn)
                    {
                        objectAction.LevelObject.VisualTransform.gameObject.SetActive(true);
                        aliveObjects.Add(objectAction.LevelObject);
                    }
                    else if (objectAction.ActionType == ObjectActionType.Kill)
                    {
                        objectAction.LevelObject.VisualTransform.gameObject.SetActive(false);
                        aliveObjects.Remove(objectAction.LevelObject);
                    }
                    index++;
                }
            }
            else // If time is going in backward direction
            {
                while (index > 0 && objectActions[index - 1].Time >= time)
                {
                    LevelObjectAction objectAction = objectActions[index - 1];
                    if (objectAction.ActionType == ObjectActionType.Spawn)
                    {
                        objectAction.LevelObject.VisualTransform.gameObject.SetActive(false);
                        aliveObjects.Remove(objectAction.LevelObject);
                    }
                    else if (objectAction.ActionType == ObjectActionType.Kill)
                    {
                        objectAction.LevelObject.VisualTransform.gameObject.SetActive(true);
                        aliveObjects.Add(objectAction.LevelObject);
                    }
                    index--;
                }
            }

            lastAudioTime = time;

            // We update the objects asynchronously. This is entire optional and can be removed.
            int workersCount = ThreadManager.NumberOfWorkers;
            for (int i = 0; i < workersCount; i++)
            {
                int workerIndex = i;
                ThreadManager.QueueWorker(i, () =>
                {
                    // Update objects.
                    // Each thread process a different subset of object.
                    for (int j = workerIndex; j < aliveObjects.Count; j += workersCount)
                    {
                        LevelObjectRef levelObject = aliveObjects[j];

                        DataManager.GameData.BeatmapObject beatmapObject = levelObject.BeatmapObject;

                        // Update the object's color
                        ColorSequence col = levelObject.ColorSequence;
                        if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty && levelObject.ObjectMaterial != null)
                        {
                            col.Update(time - levelObject.StartTime);
                            Color colValue = col.GetColor();
                            if (beatmapObject.objectType == DataManager.GameData.BeatmapObject.ObjectType.Helper)
                                colValue.a = 0.5f;

                            if (beatmapObject.shape == 4)
                            {
                                levelObject.TextComponent.color = colValue;
                            }
                            else
                            {
                                levelObject.ObjectMaterial.color = colValue;
                            }
                        }

                        // Now the transform
                        Sequence pos = levelObject.PositionSequence;
                        Sequence sca = levelObject.ScaleSequence;
                        Sequence rot = levelObject.RotationSequence;

                        pos.Update(time - levelObject.StartTime);
                        sca.Update(time - levelObject.StartTime);
                        rot.Update(time - levelObject.StartTime);

                        float[] posValues = pos.GetValues();
                        float[] scaValues = sca.GetValues();
                        float[] rotValues = rot.GetValues();

                        levelObject.BaseTransform.localPosition = new Vector3(posValues[0], posValues[1], beatmapObject.Depth * 0.1f);
                        levelObject.BaseTransform.localScale = new Vector3(scaValues[0], scaValues[1], 1f);
                        levelObject.BaseTransform.localEulerAngles = new Vector3(0f, 0f, rotValues[0]);

                        // Update the parents' transform
                        // I don't actually know how parent offset works so it might be broken.
                        for (int k = levelObject.Parents.Count - 1; k >= 0; k--)
                        {
                            ParentObjectRef parentObject = levelObject.Parents[k];

                            Sequence pPos = parentObject.PositionSequence;
                            Sequence pSca = parentObject.ScaleSequence;
                            Sequence pRot = parentObject.RotationSequence;

                            float posOffset = parentObject.PositionOffset;
                            float scaOffset = parentObject.ScaleOffset;
                            float rotOffset = parentObject.RotationOffset;

                            if (pPos != null)
                            {
                                pPos.Update(time - parentObject.StartTime - posOffset);
                                float[] pPosValues = pPos.GetValues();
                                parentObject.ObjectTransform.localPosition = new Vector3(pPosValues[0], pPosValues[1], parentObject.LocalDepth);
                            }

                            if (pSca != null)
                            {
                                pSca.Update(time - parentObject.StartTime - scaOffset);
                                float[] pScaValues = pSca.GetValues();
                                parentObject.ObjectTransform.localScale = new Vector3(pScaValues[0], pScaValues[1], 1f);
                            }

                            if (pRot != null)
                            {
                                pRot.Update(time - parentObject.StartTime - rotOffset);
                                float[] pRotValues = pRot.GetValues();
                                parentObject.ObjectTransform.localEulerAngles = new Vector3(0f, 0f, pRotValues[0]);
                            }
                        }
                    }
                });
            }

            // Now start all the object's processing
            ThreadManager.StartAllWorkers();

            // Spinwait until everything is done
            while (!ThreadManager.AllDone()) ;

            return false; // Return false to skip original method
            // This method basically replaces the entire old pipeline and replace it with a more optimized version
        }

        public static void InitLevel()
        {
            index = 0;
            lastAudioTime = 0f;

            sequencesLookup = new Dictionary<string, SequencesPair>();

            aliveObjects = new List<LevelObjectRef>();
            levelObjects = new List<LevelObjectRef>();

            objectActions = new List<LevelObjectAction>();

            foreach (DataManager.GameData.BeatmapObject beatmapObject in BeatmapObjectsLookup.Values)
            {
                // We preprocess the sequence in order to get consistent random values
                sequencesLookup.Add(beatmapObject.id, new SequencesPair
                {
                    ObjectStartTime = beatmapObject.StartTime,
                    ObjectKillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0f, true),
                    PositionSequence = GetPositionSequence(beatmapObject),
                    ScaleSequence = GetScaleSequence(beatmapObject),
                    RotationSequence = GetRotationSequence(beatmapObject)
                });
            }

            // For every object, we initialize the entire object tree.
            // We spawn everything at start, with everything being inactive.
            // According to many benchmarks, it also doesn't take a lot of memory.
            // Setting a bool is definitely way faster than instantiate and/or pooling.
            // Inactive objects has so little performance penalty it's negligible.
            foreach (DataManager.GameData.BeatmapObject beatmapObject in BeatmapObjectsLookup.Values)
            {
                // Since empty objects are not visible anyway, there is no point initializing its tree.
                if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty)
                {
                    InitObjectTree(beatmapObject);
                }
            }

            sequencesLookup.Clear();
            BeatmapObjectsLookup.Clear();

            // Now put all the objects in action
            // Note that we add a small amount to kill time, this is to ensure that kill time is always > start time.
            for (int i = 0; i < levelObjects.Count; i++)
            {
                LevelObjectRef levelObject = levelObjects[i];

                objectActions.Add(new LevelObjectAction
                {
                    Time = levelObject.StartTime,
                    ActionType = ObjectActionType.Spawn,
                    LevelObject = levelObject
                });

                objectActions.Add(new LevelObjectAction
                {
                    Time = levelObject.KillTime + 0.0001f,
                    ActionType = ObjectActionType.Kill,
                    LevelObject = levelObject
                });
            }

            // Now sort all the actions
            objectActions.Sort((x, y) => x.Time.CompareTo(y.Time));
        }

        private static void InitObjectTree(DataManager.GameData.BeatmapObject beatmapObject)
        {
            // We recursively loop back to the top of the tree and start from the top of the tree.
            List<ParentObjectRef> parents = new List<ParentObjectRef>();
            Transform parent = ObjectManager.inst.objectParent.transform;
            if (BeatmapObjectsLookup.TryGetValue(beatmapObject.parent, out var value))
            {
                bool pos = beatmapObject.GetParentType(0);
                bool sca = beatmapObject.GetParentType(1);
                bool rot = beatmapObject.GetParentType(2);
                parent = InitObjectParentRecursively(value, beatmapObject, parents, ref pos, ref sca, ref rot);
            }

            Transform baseTransform = UnityEngine.Object.Instantiate(ObjectManager.inst.objectPrefabs[beatmapObject.shape].options[beatmapObject.shapeOption], parent).transform;
            Transform visualTransform = baseTransform.GetChild(0);

            SequencesPair sequences = sequencesLookup[beatmapObject.id];

            // We deep copy because we don't want the same instance of sequence being accessed at the same time on different threads
            Sequence posSeq = sequences.PositionSequence.DeepCopy();
            Sequence scaSeq = sequences.ScaleSequence.DeepCopy();
            Sequence rotSeq = sequences.RotationSequence.DeepCopy();

            posSeq.Update(-1f);
            scaSeq.Update(-1f);
            rotSeq.Update(-1f);

            float[] posValues = posSeq.GetValues();
            float[] scaValues = scaSeq.GetValues();
            float[] rotValues = rotSeq.GetValues();

            // Set the transforms in advance to account for objects that spawn and despawn immediately
            baseTransform.localPosition = new Vector3(posValues[0], posValues[1], beatmapObject.Depth * 0.1f);
            baseTransform.localScale = new Vector3(scaValues[0], scaValues[1], 1f);
            baseTransform.localEulerAngles = new Vector3(0f, 0f, rotValues[0]);

            visualTransform.localPosition = beatmapObject.origin;
            visualTransform.localScale = Vector3.one;
            visualTransform.localRotation = Quaternion.identity;

            if (beatmapObject.shape == 4)
            {
                TextMeshPro tmp = visualTransform.GetComponent<TextMeshPro>();
                tmp.enabled = true;
                tmp.SetText(beatmapObject.text);
                tmp.color = Color.white;
            }

            Renderer renderer = visualTransform.GetComponent<Renderer>();

            if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty)
            {
                renderer.enabled = true;
                renderer.material.color = new Color(0f, 0f, 0f,
                    beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Helper
                    ? 1f : 0.5f);
            }
            else
            {
                renderer.enabled = false;
            }

            if (beatmapObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Normal)
            {
                Collider2D collider = visualTransform.GetComponent<Collider2D>();

                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }
            }

            baseTransform.gameObject.SetActive(true);
            visualTransform.gameObject.SetActive(false);

            levelObjects.Add(new LevelObjectRef
            {
                StartTime = beatmapObject.StartTime,
                KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0f, true) + 0.00001f,
                TextComponent = beatmapObject.shape == 4 ? visualTransform.GetComponent<TextMeshPro>() : null,
                ObjectMaterial = renderer != null ? renderer.material : null,
                BaseTransform = baseTransform,
                VisualTransform = visualTransform,
                BeatmapObject = beatmapObject,
                PositionSequence = posSeq,
                ScaleSequence = scaSeq,
                RotationSequence = rotSeq,
                ColorSequence = GetColorSequence(beatmapObject),
                Parents = parents
            });
        }

        private static Transform InitObjectParentRecursively(
            DataManager.GameData.BeatmapObject beatmapObject, 
            DataManager.GameData.BeatmapObject baseBeatmapObject,
            List<ParentObjectRef> parentObjects,
            ref bool pos,
            ref bool sca,
            ref bool rot)
        {
            bool animPos = pos;
            bool animSca = sca;
            bool animRot = rot;

            Transform parent = ObjectManager.inst.objectParent.transform;
            if (BeatmapObjectsLookup.TryGetValue(beatmapObject.parent, out var value))
            {
                pos = pos && beatmapObject.GetParentType(0);
                sca = sca && beatmapObject.GetParentType(1);
                rot = rot && beatmapObject.GetParentType(2);

                parent = InitObjectParentRecursively(value, baseBeatmapObject, parentObjects, ref pos, ref sca, ref rot);
            }

            Transform currentTransform = new GameObject().transform;
            currentTransform.SetParent(parent);

            SequencesPair sequences = sequencesLookup[beatmapObject.id];

            // We deep copy because we don't want the same instance of sequence being accessed at the same time on different threads
            Sequence posSeq = sequences.PositionSequence.DeepCopy();
            Sequence scaSeq = sequences.ScaleSequence.DeepCopy();
            Sequence rotSeq = sequences.RotationSequence.DeepCopy();

            posSeq.Update(-1f);
            scaSeq.Update(-1f);
            rotSeq.Update(-1f);

            float[] posValues = posSeq.GetValues();
            float[] scaValues = scaSeq.GetValues();
            float[] rotValues = rotSeq.GetValues();

            // Set the transforms in advance to account for objects that spawn and despawn immediately
            currentTransform.localPosition = animPos ? new Vector3(posValues[0], posValues[1], baseBeatmapObject.Depth * 0.0005f) : new Vector3(0f, 0f, baseBeatmapObject.Depth * 0.0005f);
            currentTransform.localScale = animSca ? new Vector3(scaValues[0], scaValues[1], 1f) : Vector3.one;
            currentTransform.localEulerAngles = animRot ? new Vector3(0f, 0f, rotValues[0]) : Vector3.zero;

            parentObjects.Add(new ParentObjectRef
            {
                ObjectTransform = currentTransform,
                StartTime = beatmapObject.StartTime,
                KillTime = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0f, true),
                LocalDepth = baseBeatmapObject.Depth * 0.0005f,
                PositionOffset = beatmapObject.getParentOffset(0),
                ScaleOffset = beatmapObject.getParentOffset(1),
                RotationOffset = beatmapObject.getParentOffset(2),
                PositionSequence = animPos ? posSeq : null,
                ScaleSequence = animSca ? scaSeq : null,
                RotationSequence = animRot ? rotSeq : null
            });

            return currentTransform;
        }

        private static Sequence GetPositionSequence(DataManager.GameData.BeatmapObject beatmapObject)
        {
            List<DataManager.GameData.EventKeyframe> posEvents = beatmapObject.events[0];
            posEvents.Sort((x, y) => x.eventTime.CompareTo(y.eventTime));
            LeaderAnimator.Keyframe[] posKfs = new LeaderAnimator.Keyframe[posEvents.Count];

            for (int i = 0; i < posEvents.Count; i++)
            {
                var posEvent = posEvents[i];

                float x = posEvent.eventValues[0];
                float y = posEvent.eventValues[1];

                if (posEvent.random != 0)
                {
                    Vector2 newVec = ObjectManager.inst.RandomVector2Parser(posEvent);
                    x = newVec.x;
                    y = newVec.y;
                }

                posKfs[i] = new LeaderAnimator.Keyframe()
                {
                    Values = new float[] { x, y },
                    Time = posEvent.eventTime,
                    Easing = stringToEasings[posEvent.curveType.Name]
                };
            }

            return new Sequence(posKfs, 2);
        }

        private static Sequence GetScaleSequence(DataManager.GameData.BeatmapObject beatmapObject)
        {
            List<DataManager.GameData.EventKeyframe> scaEvents = beatmapObject.events[1];
            scaEvents.Sort((x, y) => x.eventTime.CompareTo(y.eventTime));
            LeaderAnimator.Keyframe[] scaKfs = new LeaderAnimator.Keyframe[scaEvents.Count];

            for (int i = 0; i < scaEvents.Count; i++)
            {
                var scaEvent = scaEvents[i];

                float x = scaEvent.eventValues[0];
                float y = scaEvent.eventValues[1];

                if (scaEvent.random != 0)
                {
                    Vector2 newVec = ObjectManager.inst.RandomVector2Parser(scaEvent);
                    x = newVec.x;
                    y = newVec.y;
                }

                scaKfs[i] = new LeaderAnimator.Keyframe()
                {
                    Values = new float[] { x, y },
                    Time = scaEvent.eventTime,
                    Easing = stringToEasings[scaEvent.curveType.Name]
                };
            }

            return new Sequence(scaKfs, 2);
        }

        private static Sequence GetRotationSequence(DataManager.GameData.BeatmapObject beatmapObject)
        {
            List<DataManager.GameData.EventKeyframe> rotEvents = beatmapObject.events[2];
            rotEvents.Sort((x, y) => x.eventTime.CompareTo(y.eventTime));
            LeaderAnimator.Keyframe[] rotKfs = new LeaderAnimator.Keyframe[rotEvents.Count];

            float lastRot = 0f;
            for (int i = 0; i < rotEvents.Count; i++)
            {
                var rotEvent = rotEvents[i];

                float x = rotEvent.eventValues[0];

                if (rotEvent.random != 0)
                {
                    x = ObjectManager.inst.RandomFloatParser(rotEvent);
                }

                x += lastRot;

                rotKfs[i] = new LeaderAnimator.Keyframe()
                {
                    Values = new float[] { x },
                    Time = rotEvent.eventTime,
                    Easing = stringToEasings[rotEvent.curveType.Name]
                };

                lastRot = x;
            }

            return new Sequence(rotKfs, 1);
        }

        private static ColorSequence GetColorSequence(DataManager.GameData.BeatmapObject beatmapObject)
        {
            List<DataManager.GameData.EventKeyframe> colEvents = beatmapObject.events[3];
            colEvents.Sort((x, y) => x.eventTime.CompareTo(y.eventTime));
            ColorKeyframe[] colKfs = new ColorKeyframe[colEvents.Count];

            for (int i = 0; i < colKfs.Length; i++)
            {
                var colEvent = colEvents[i];

                colKfs[i] = new ColorKeyframe
                {
                    Value = (int)colEvent.eventValues[0],
                    Time = colEvent.eventTime,
                    Easing = stringToEasings[colEvent.curveType.Name]
                };
            }

            return new ColorSequence(colKfs);
        }
    }
}
