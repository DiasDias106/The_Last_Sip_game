using UnityEngine;

public class Level2PuzzleManager : MonoBehaviour
{
    void Start()
    {
        SetupPuzzle();
        AddInteractionDetectorToPlayer();
    }

    void SetupPuzzle()
    {
        // ── Door 1: 4 top levers → light middle tower fires → open middle door ──
        var door1Levers = new[]
        {
            GetOrAddLever("door1_l1"),
            GetOrAddLever("door1_l2"),
            GetOrAddLever("door1_l3"),
            GetOrAddLever("door1_l4"),
        };

        var middleFires = new[]
        {
            GetFire("COMP_PROP_brazier_dungeon_03"),
            GetFire("COMP_PROP_brazier_dungeon_03 (1)"),
            GetFire("COMP_PROP_brazier_dungeon_03 (2)"),
            GetFire("COMP_PROP_brazier_dungeon_03 (3)"),
        };

        var middleDoor = GetOrAddDoorOpener("SM_PROP_cage_door_dungeon_01 (1)", new Vector3(0f, 4f, 0f), 1.5f);

        foreach (var f in middleFires)
            if (f != null) f.SetActive(false);

        for (int i = 0; i < door1Levers.Length; i++)
        {
            if (door1Levers[i] == null) continue;
            int   idx      = i;
            var   fire     = middleFires[idx];
            var   door     = middleDoor;
            var   levers   = door1Levers;

            door1Levers[i].onActivated.AddListener(() =>
            {
                if (fire != null) fire.SetActive(true);

                bool allOn = true;
                foreach (var l in levers)
                    if (l != null && !l.IsActivated) { allOn = false; break; }
                if (allOn && door != null) door.Open();
            });
        }

        // ── Door 2: 2 levers → light big-door fires + open door2 ──
        var door2Levers = new[]
        {
            GetOrAddLever("door2_l1"),
            GetOrAddLever("doo2_l2"),
        };

        var bigDoorFires = new[]
        {
            GetFire("COMP_PROP_brazier_dungeon_03 (4)"),
            GetFire("COMP_PROP_brazier_dungeon_03 (5)"),
        };

        var door2 = GetOrAddDoorOpener("door2", new Vector3(0f, 4f, 0f), 1.5f);

        foreach (var f in bigDoorFires)
            if (f != null) f.SetActive(false);

        foreach (var lever in door2Levers)
        {
            if (lever == null) continue;
            var fires  = bigDoorFires;
            var door   = door2;
            var levers = door2Levers;

            lever.onActivated.AddListener(() =>
            {
                foreach (var f in fires)
                    if (f != null) f.SetActive(true);

                bool allOn = true;
                foreach (var l in levers)
                    if (l != null && !l.IsActivated) { allOn = false; break; }
                if (allOn && door != null) door.Open();
            });
        }

        // ── Big door: central lever ──
        var bigDoorLever = GetOrAddLever("big_door_lever");
        var bigDoor      = GetOrAddDoorOpener("SM_PROP_cage_door_dungeon_01 (2)", new Vector3(0f, 6f, 0f), 2f);

        if (bigDoorLever != null)
            bigDoorLever.onActivated.AddListener(() => { if (bigDoor != null) bigDoor.Open(); });
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
        opener.openOffset   = offset;
        opener.openDuration = duration;
        return opener;
    }

    GameObject GetFire(string brazierName)
    {
        var go = GameObject.Find(brazierName);
        if (go == null) return null;
        var fire = go.transform.Find("P_PROP_brazier_dungeon_02");
        return fire?.gameObject;
    }
}
