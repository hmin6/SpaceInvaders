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

    static DialogueLine Op(string text) => DialogueLine.Operator(text);
    static DialogueLine Pod(string text) => DialogueLine.Pod(text);

    // Failure conversation shared by SIM 01 and SIM 02
    static DialogueLine[] StandardFailure() => new[]
    {
        Op("Don't worry. Nobody passes on the first try."),
        Op("...Almost nobody."),
        Pod("Proposal: Retry."),
    };

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
            darkBulletChance: 0f,
            briefingSystemLine: "Proposal: Unit No. 11S proceed to combat simulation 01.",
            briefing: new[]
            {
                Op("Good morning, 11S. I'm Operator 17O. I'll be monitoring your certification."),
                Op("Your targets are machine lifeforms, reconstructed from archived combat data."),
                Pod("Report: Pod 217 online. Assigned to support Unit 11S."),
                Pod("Proposal: Destroy targets to charge Pod Program: Laser."),
                Op("The Commander reads these logs, so no pressure."),
            },
            debriefSystemLine: "Analysis: Simulation 01 cleared. Combat parameters within tolerance.",
            debrief: new[]
            {
                Op("Nice work, 11S. Your reaction time is above the unit average."),
                Op("...I wasn't supposed to tell you that."),
                Pod("Report: Operator 17O has disclosed restricted evaluation data. Logging."),
                Op("...Pod, you don't have to log everything."),
                Pod("Correction: Pod 217 is required to log everything."),
            },
            failureDialogue: StandardFailure()));

        LevelData sim02 = CreateOrUpdate("SIM02_AdvancedCombat", overwrite, level => level.EditorInitialize(
            levelName: "SIM 02: ADVANCED COMBAT",
            isAnomaly: false,
            columns: 10, rows: 4,
            baseSpeed: 6f, maxSpeedMultiplier: 4.5f, speedUpExponent: 1.5f,
            baseFireRate: 1.9f, maxFireRate: 3.9f, aimedShotChance: .45f, alienBulletSpeed: 10f,
            darkBulletChance: .2f,
            briefingSystemLine: "ALERT: Enemy aggression parameters increased.",
            briefing: new[]
            {
                Op("This simulation uses combat data recorded on the surface."),
                Op("It was recorded before you were activated."),
                Pod("Query: Current status of the unit that recorded this data?"),
                Op("...That's not relevant to your certification."),
                Op("Stay focused, 11S."),
            },
            debriefSystemLine: "Analysis: Simulation 02 cleared. Adaptation rate: high.",
            debrief: new[]
            {
                Op("Good work, 11S. Next stop is the surface."),
                Op("Earth is... well. You'll see it soon."),
                Pod("Query: Should Unit 11S be informed of current surface conditions?"),
                Op("Not yet."),
                Pod("Report: Request noted."),
            },
            failureDialogue: StandardFailure()));

        LevelData sim03 = CreateOrUpdate("SIM03_Anomaly", overwrite, level => level.EditorInitialize(
            levelName: "SIM 03: ANOMALY",
            isAnomaly: true,
            columns: 10, rows: 4,
            baseSpeed: 7f, maxSpeedMultiplier: 4.5f, speedUpExponent: 1.4f,
            baseFireRate: 2.1f, maxFireRate: 4.0f, aimedShotChance: .5f, alienBulletSpeed: 10.5f,
            darkBulletChance: .35f,
            briefingSystemLine: "ALERT: Simulation 03 contains unverified data.",
            briefing: new[]
            {
                Op("11S, wait. This one wasn't on the schedule."),
                Op("The enemy data doesn't match any training record I have access to."),
                Op("Pod, can you identify the source?"),
                Pod("Negative. Access restricted."),
                Op("...Restricted? By who?"),
                Pod("Information unavailable."),
                Op("I've reported it. I'm told it's within parameters."),
                Op("Just... stay sharp, 11S."),
            },
            // The certification conversation (GameManager) replaces SIM 03's debrief
            debriefSystemLine: "Analysis: Simulation 03 cleared. Anomalous data: purged.",
            debrief: new DialogueLine[0],
            failureTitle: "Simulation Integrity Compromised",
            failureSystemLine: "ALERT: Connection to unit interrupted.",
            failureDialogue: new[]
            {
                Op("11S? 11S, respond--"),
                Op("...Your vitals are stable. I'm pulling you out."),
                Pod("Report: Unit 11S data intact. Simulation will be reinitialized."),
                Op("Within parameters."),
                Op("...That's what they keep telling me."),
            },
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
