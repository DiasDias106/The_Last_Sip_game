using UnityEngine;

public class VisualAnimationEventReceiver : MonoBehaviour
{
    // The RPG pack includes footstep animation events. We consume them here to avoid console spam.
    public void FootL() { }
    public void FootR() { }
    public void FootL(AnimationEvent _) { }
    public void FootR(AnimationEvent _) { }
}
