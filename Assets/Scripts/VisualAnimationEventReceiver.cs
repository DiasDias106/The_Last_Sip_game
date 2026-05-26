using UnityEngine;

public class VisualAnimationEventReceiver : MonoBehaviour
{
    // Consume animation events from the RPG pack clips to avoid console spam.
    public void FootL() { }
    public void FootR() { }
    public void FootL(AnimationEvent _) { }
    public void FootR(AnimationEvent _) { }

    // Attack clips fire a 'Hit' event at the impact frame — consumed here so the
    // damage is handled by the timer in UserImportedPlayerController instead.
    public void Hit() { }
    public void Hit(AnimationEvent _) { }
}
