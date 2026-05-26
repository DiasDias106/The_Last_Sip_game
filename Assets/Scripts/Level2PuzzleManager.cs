using System.Collections;
using UnityEngine;

public class Level2PuzzleManager : MonoBehaviour
{
    void Start()
    {
        SetupPuzzle();
        AddInteractionDetectorToPlayer();
    }

    void OpenBigGateway()
    {
        var bigDoorRoot = GameObject.Find("big_door");
        if (bigDoorRoot == null)
        {
            Debug.LogWarning("[Level2Puzzle] 'big_door' root not found.");
            return;
        }

        OpenDoorLeafByRotation(bigDoorRoot.transform, "MOD_Gateway_Door_01_large_door_L", -90f);
        OpenDoorLeafByRotation(bigDoorRoot.transform, "MOD_Gateway_Door_01_large_door_R", 90f);
        HideBigDoorPlank(bigDoorRoot.transform, "MOD_Gateway_Door_01_large_plank_01");
        HideBigDoorPlank(bigDoorRoot.transform, "MOD_Gateway_Door_01_large_plank_02");
    }

    void OpenDoorLeafByRotation(Transform root, string objectName, float yAngle)
    {
        var t = FindChildRecursive(root, objectName);
        if (t == null)
        {
            Debug.LogWarning($"[Level2Puzzle] Big door leaf '{objectName}' not found.");
            return;
        }

        Vector3 euler = t.localEulerAngles;
        t.localRotation = Quaternion.Euler(euler.x, yAngle, euler.z);

        foreach (var col in t.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }

    void HideBigDoorPlank(Transform root, string objectName)
    {
        var t = FindChildRecursive(root, objectName);
        if (t == null)
        {
            Debug.LogWarning($"[Level2Puzzle] Big door plank '{objectName}' not found.");
            return;
        }

        foreach (var col in t.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    void SetupPuzzle()
    {
        // Door 1: 4 top levers -> light middle fires -> open door1
        var door1Levers = new[]
        {
            GetOrAddLever("door1_l1"),
            GetOrAddLever("door1_l2"),
            GetOrAddLever("door1_l3"),
            GetOrAddLever("door1_l4"),
        };

        var middleFires = new[]
        {
            GetFireFx("COMP_PROP_brazier_dungeon_03"),
            GetFireFx("COMP_PROP_brazier_dungeon_03 (1)"),
            GetFireFx("COMP_PROP_brazier_dungeon_03 (2)"),
            GetFireFx("COMP_PROP_brazier_dungeon_03 (3)"),
        };

        var middleDoor = GetOrAddDoorOpener("door1", new Vector3(0f, 4f, 0f), 1.5f);
        if (middleDoor != null) middleDoor.hideOnOpen = true;

        for (int i = 0; i < middleFires.Length; i++)
            SetBrazierLit(middleFires[i], false);

        for (int i = 0; i < door1Levers.Length; i++)
        {
            if (door1Levers[i] == null) continue;
            int idx = i;
            var fire = middleFires[idx];
            var door = middleDoor;
            var levers = door1Levers;

            door1Levers[i].onActivated.AddListener(() =>
            {
                SetBrazierLit(fire, true);

                bool allOn = true;
                foreach (var l in levers)
                    if (l != null && !l.IsActivated) { allOn = false; break; }
                if (allOn && door != null) door.Open();
            });
        }

        // Door 2: 2 lower levers -> light brazier (4)/(5) only when both are active -> open door2
        var door2Levers = new[]
        {
            GetOrAddLever("door2_l1"),
            GetOrAddLever("door2_l2"),
        };

        var bigDoorFires = new[]
        {
            GetFireFx("COMP_PROP_brazier_dungeon_03 (4)"),
            GetFireFx("COMP_PROP_brazier_dungeon_03 (5)"),
        };

        var door2 = GetOrAddDoorOpener("door2", new Vector3(0f, 4f, 0f), 1.5f);
        if (door2 != null) door2.hideOnOpen = true;

        for (int i = 0; i < bigDoorFires.Length; i++)
            SetBrazierLit(bigDoorFires[i], false);

        foreach (var lever in door2Levers)
        {
            if (lever == null) continue;
            var door = door2;
            var levers = door2Levers;
            var fires = bigDoorFires;

            lever.onActivated.AddListener(() =>
            {
                bool allOn = true;
                foreach (var l in levers)
                    if (l != null && !l.IsActivated) { allOn = false; break; }

                foreach (var f in fires)
                    SetBrazierLit(f, allOn);

                if (allOn && door != null) door.Open();
            });
        }

        // Big door: central lever -> only opens big door
        var bigDoorLever = GetOrAddLever("big_door_lever");
        var door1ForReset = middleDoor;

        if (bigDoorLever != null)
            bigDoorLever.onActivated.AddListener(() =>
            {
                OpenBigGateway();
                if (door1ForReset != null) door1ForReset.CloseAndReset();
                StartCoroutine(FadeTeleport(door1ForReset));
            });
    }

    void AddInteractionDetectorToPlayer()
    {
        var controller = FindObjectOfType<UserImportedPlayerController>();
        if (controller == null) return;
        if (controller.GetComponent<PlayerInteractionDetector>() == null)
            controller.gameObject.AddComponent<PlayerInteractionDetector>();
    }

    LeverInteractable GetOrAddLever(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning($"[Level2Puzzle] '{objectName}' not found"); return null; }
        return go.GetComponent<LeverInteractable>() ?? go.AddComponent<LeverInteractable>();
    }

    DoorOpener GetOrAddDoorOpener(string objectName, Vector3 offset, float duration)
    {
        var go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning($"[Level2Puzzle] '{objectName}' not found"); return null; }
        var opener = go.GetComponent<DoorOpener>() ?? go.AddComponent<DoorOpener>();
        opener.openOffset = offset;
        opener.openDuration = duration;
        return opener;
    }

    GameObject GetFireFx(string brazierName)
    {
        var go = GameObject.Find(brazierName);
        if (go == null) return null;
        return go;
    }

    void SetBrazierLit(GameObject brazierOrFireRoot, bool isLit)
    {
        if (brazierOrFireRoot == null) return;

        // Keep brazier mesh visible at all times.
        var brazierMesh = FindChildRecursive(brazierOrFireRoot.transform, "P_PROP_brazier_dungeon_02");
        if (brazierMesh != null) brazierMesh.gameObject.SetActive(true);

        var fireFx = FindChildRecursive(brazierOrFireRoot.transform, "P_FX_fire_dungeon");
        if (fireFx != null) fireFx.gameObject.SetActive(isLit);

        var steamFx = FindChildRecursive(brazierOrFireRoot.transform, "P_FX_steam_dungeon");
        if (steamFx != null) steamFx.gameObject.SetActive(isLit);

        var lightFx = FindChildRecursive(brazierOrFireRoot.transform, "P_pointlight_fire_dungeon");
        if (lightFx != null) lightFx.gameObject.SetActive(isLit);
    }

    Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }

    IEnumerator FadeTeleport(DoorOpener door1)
    {
        var fader = ScreenFader.GetOrCreate();

        yield return fader.FadeOut(0.5f);

        if (door1 != null)
        {
            var player = FindObjectOfType<UserImportedPlayerController>();
            if (player != null)
            {
                Vector3 target = door1.transform.position + Vector3.up * 1.6f;
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = target;
                if (cc != null) cc.enabled = true;
            }
        }

        yield return new WaitForSeconds(0.2f);
        yield return fader.FadeIn(0.6f);
    }
}
