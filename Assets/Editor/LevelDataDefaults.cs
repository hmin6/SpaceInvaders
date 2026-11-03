using UnityEditor;
using UnityEngine;

// Editor-only menus for the three simulation LevelData assets in Assets/Levels:
//   Tools > YoRHa > Create Default Levels            creates any that are missing; existing ones are untouched
//   Tools > YoRHa > Reset Default Levels (Overwrite) rewrites all three with the values and dialogue below
// Resetting updates the existing assets in place, so GameManager's references to them keep working.
public static class LevelDataDefaults
{
    const string Folder = "Assets/Levels";

    [MenuItem("Tools/YoRHa/Create Default Levels")]
    static void CreateDefaultLevels()
    {
        Build(false);
    }

    [MenuItem("Tools/YoRHa/Reset Default Levels (Overwrite)")]
    static void ResetDefaultLevels()
    {
        if (EditorUtility.DisplayDialog("Reset default levels",
            "Overwrite SIM 01-03 in " + Folder + " with the default values and dialogue? Any edits to them will be lost.",
            "Overwrite", "Cancel"))
        {
            Build(true);
        }
    }

    static void Build(bool overwrite)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Levels");
        }

        LevelData sim01 = CreateOrUpdate("SIM01_BasicCombat", overwrite, level => level.EditorInitialize(
            levelName: "SIM 01: BASIC COMBAT",
            isAnomaly: false,
            columns: 10, rows: 3,
            baseSpeed: 5f, maxSpeedMultiplier: 5f, speedUpExponent: 1.5f,
            baseFireRate: 1.5f, maxFireRate: 3.5f, aimedShotChance: .35f, alienBulletSpeed: 9f,
            briefingSystemLine: "Proposal: Unit No. 11S proceed to combat simulation 01.",
            briefing:
                "Good morning, 11S. This is Operator 17O.\n" +
                "Targets are machine lifeforms rebuilt from archive data.\n" +
                "Your Pod is online. Destroy enough targets to charge its laser.\n" +
                "...The Commander reads these logs, so no pressure.",
            debriefSystemLine: "Analysis: Simulation 01 cleared. Combat parameters within tolerance.",
            debrief:
                "Nice work, 11S. Your reaction time is above the unit average.\n" +
                "...I wasn't supposed to tell you that.\n" +
                "Proceed to the next simulation."));

        LevelData sim02 = CreateOrUpdate("SIM02_AdvancedCombat", overwrite, level => level.EditorInitialize(
            levelName: "SIM 02: ADVANCED COMBAT",
            isAnomaly: false,
            columns: 10, rows: 4,
            baseSpeed: 6f, maxSpeedMultiplier: 4.5f, speedUpExponent: 1.5f,
            baseFireRate: 1.9f, maxFireRate: 3.9f, aimedShotChance: .45f, alienBulletSpeed: 10f,
            briefingSystemLine: "ALERT: Enemy aggression parameters increased.",
            briefing:
                "Simulation 02 raises enemy speed and rate of fire.\n" +
                "This combat data was recorded before you were activated.\n" +
                "Keep moving. Don't let them pin you down.",
            debriefSystemLine: "Analysis: Simulation 02 cleared. Adaptation rate: high.",
            debrief:
                "You're adapting faster than the model predicted.\n" +
                "One simulation left. Then you're cleared for deployment.\n" +
                "Earth is... well. You'll see it soon."));

        LevelData sim03 = CreateOrUpdate("SIM03_Anomaly", overwrite, level => level.EditorInitialize(
            levelName: "SIM 03: ANOMALY",
            isAnomaly: true,
            columns: 10, rows: 4,
            baseSpeed: 7f, maxSpeedMultiplier: 4.5f, speedUpExponent: 1.4f,
            baseFireRate: 2.1f, maxFireRate: 4.0f, aimedShotChance: .5f, alienBulletSpeed: 10.5f,
            briefingSystemLine: "ALERT: Simulation 03 contains unverified data.",
            briefing:
                "This one wasn't on the schedule. It was added to your queue this morning.\n" +
                "The enemy data doesn't match any training record I have access to.\n" +
                "I've reported it. I'm told it's within parameters.\n" +
                "Just... stay sharp, 11S.",
            debriefSystemLine: "Analysis: Simulation 03 cleared. Anomalous data: purged.",
            debrief:
                "That's all of them. Certification is complete.\n" +
                "Unit No. 11 Type S, you are cleared for deployment to Earth.\n" +
                "I'll be your Operator down there, too.\n" +
                "...If that's all right.\n" +
                "Glory to Mankind.",
            failureTitle: "Simulation Integrity Compromised",
            failureSystemLine: "ALERT: Connection to unit interrupted.",
            failureMessage:
                "11S? 11S, respond—\n" +
                "...Your vitals are stable. I'm pulling you out.\n" +
                "Resetting simulation.\n" +
                "...I'm told this is within parameters.",
            failureMenuLabel: "[ Reinitialize ]"));

        AssetDatabase.SaveAssets();
        Selection.objects = new Object[] { sim01, sim02, sim03 };
        EditorGUIUtility.PingObject(sim01);
        Debug.Log("YoRHa levels ready in " + Folder + ". GameManager > Levels should list them in order.");
    }

    static LevelData CreateOrUpdate(string fileName, bool overwrite, System.Action<LevelData> initialize)
    {
        string path = Folder + "/" + fileName + ".asset";
        LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (existing != null)
        {
            if (overwrite)
            {
                Undo.RecordObject(existing, "Reset default level");
                initialize(existing);
                EditorUtility.SetDirty(existing);
                Debug.Log(path + " reset to defaults.");
            }
            else
            {
                Debug.Log(path + " already exists; left unchanged.");
            }
            return existing;
        }

        LevelData level = ScriptableObject.CreateInstance<LevelData>();
        initialize(level);
        AssetDatabase.CreateAsset(level, path);
        return level;
    }
}
