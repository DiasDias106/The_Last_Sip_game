using UnityEngine;

[CreateAssetMenu(fileName = "RuntimeAnimationLibrary", menuName = "The Last Sip/Runtime Animation Library")]
public class RuntimeAnimationLibrary : ScriptableObject
{
    [Header("Beco Noite")]
    [SerializeField] private AnimationClip drunkIdle;
    [SerializeField] private AnimationClip drunkWalk;
    [SerializeField] private AnimationClip drunkWalkNoRootMotion;

    [Header("Consumables")]
    [SerializeField] private AnimationClip moonshineIntro;
    [SerializeField] private AnimationClip moonshineLoop;

    public AnimationClip FindClip(string clipName)
    {
        return clipName switch
        {
            "MCU_am_StandDrunk_Idle_01" => drunkIdle,
            "MCU_am_Drunk_Loco_Walk_01" => drunkWalk,
            "MCU_am_Drunk_Loco_Walk_01_NoRM" => drunkWalkNoRootMotion,
            "MCU_am_Stand_Trans_Moonshine" => moonshineIntro,
            "MCU_am_Moonshine_01_Drink_Loop" => moonshineLoop,
            _ => null
        };
    }
}
