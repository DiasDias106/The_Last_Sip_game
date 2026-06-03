using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class LimoNightTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject limoPrefab;
    [SerializeField] private Transform limoSpawnPoint;
    [SerializeField] private Transform playerImpactPoint;
    [SerializeField] private Transform limoExitPoint;
    [SerializeField] private float roadZOffset = -4.5f;
    [SerializeField] private float limoSpawnYOffset = 0.35f;
    [SerializeField] private Vector3 limoScale = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 limoDriveEuler = new Vector3(270f, 0f, 0f);
    [SerializeField] private Material limoMaterialOverride;
    [SerializeField] private GameObject sceneLimoObject;

    [Header("Sequence")]
    [SerializeField] private float playerWalkSpeed = 3.75f;
    [SerializeField] private float limoSpeed = 13f;
    [SerializeField] private float killDistance = 1.3f;
    [SerializeField] private float impactPointDistance = 1.6f;
    [SerializeField] private float limoStartDelay = 0.15f;
    [SerializeField] private float preImpactLookDelay = 0.35f;
    [SerializeField] private float limoImpactTimeout = 4f;
    [SerializeField] private float maxAutoWalkTime = 2.5f;
    [SerializeField] private float postImpactDriveTime = 1.1f;
    [SerializeField] private float cameraPitch = 16f;
    [SerializeField] private float minimumLimoVisibleLength = 7f;

    private static bool _globalSequenceStarted;
    private bool _sequenceStarted;

    private void Awake()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;

        PrepareSceneLimoObject(false);
    }

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(10f, 3f, 10f);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartSequence(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartSequence(other);
    }

    private void TryStartSequence(Collider other)
    {
        if (_sequenceStarted || _globalSequenceStarted || other == null)
        {
            return;
        }

        UserImportedPlayerController controller = other.GetComponent<UserImportedPlayerController>()
            ?? other.GetComponentInParent<UserImportedPlayerController>();
        PlayerHealth health = other.GetComponent<PlayerHealth>()
            ?? other.GetComponentInParent<PlayerHealth>();
        if (controller == null || health == null)
        {
            return;
        }

        _sequenceStarted = true;
        _globalSequenceStarted = true;
        StartCoroutine(PlayNightSequence(controller, health));
    }

    private IEnumerator PlayNightSequence(UserImportedPlayerController controller, PlayerHealth health)
    {
        controller.SetExternalControlLocked(true);
        controller.PlayExternalAnimation("walk");

        Vector3 targetPosition = GetImpactDestination(controller);
        float autoWalkTimer = 0f;
        while (Vector3.Distance(controller.transform.position, targetPosition) > 0.12f)
        {
            Vector3 direction = targetPosition - controller.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                controller.FaceDirection(direction);
                Vector3 step = direction.normalized * playerWalkSpeed * Time.deltaTime;
                if (step.sqrMagnitude > direction.sqrMagnitude)
                {
                    step = direction;
                }

                controller.transform.position += step;
                controller.PlayExternalAnimation("walk");
            }

            autoWalkTimer += Time.deltaTime;
            if (autoWalkTimer >= maxAutoWalkTime)
            {
                break;
            }

            yield return null;
        }

        controller.transform.position = new Vector3(
            targetPosition.x,
            controller.transform.position.y,
            targetPosition.z);
        controller.FaceDirection(Vector3.forward);

        controller.PlayExternalAnimation("idle");
        yield return new WaitForSeconds(limoStartDelay);

        GameObject limo = CreateLimo();
        if (limo == null)
        {
            _globalSequenceStarted = false;
            health.TakeDamage(health.CurrentHealth + health.maxHealth);
            yield break;
        }

        float driveLaneZ = controller.transform.position.z;
        Vector3 limoStartPosition = GetLimoSpawnPosition();
        limoStartPosition.z = driveLaneZ;
        if (limoStartPosition.x >= controller.transform.position.x)
        {
            limoStartPosition.x = controller.transform.position.x - 6f;
        }

        limo.transform.position = limoStartPosition;
        limo.transform.rotation = Quaternion.Euler(limoDriveEuler);

        Vector3 lookDirection = limo.transform.position - controller.transform.position;
        lookDirection.y = 0f;
        controller.FaceDirection(lookDirection);
        GameObject cameraAnchor = new GameObject("NightLimoCameraAnchor");
        Transform cameraAnchorTransform = cameraAnchor.transform;
        cameraAnchorTransform.position = controller.transform.position;
        cameraAnchorTransform.rotation = controller.transform.rotation;
        controller.SetCameraFollowOverride(cameraAnchorTransform, true, cameraPitch);
        yield return new WaitForSeconds(preImpactLookDelay);

        bool impactTriggered = false;
        float postImpactTimer = 0f;
        float impactTimer = 0f;
        while (true)
        {
            Vector3 destination = !impactTriggered
                ? new Vector3(controller.transform.position.x, limo.transform.position.y, driveLaneZ)
                : GetLimoExitDestination(driveLaneZ, limo.transform.position);
            Vector3 direction = destination - limo.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Vector3 step = direction.normalized * limoSpeed * Time.deltaTime;
                if (step.sqrMagnitude > direction.sqrMagnitude)
                {
                    step = direction;
                }

                limo.transform.position += step;
                limo.transform.rotation = Quaternion.Euler(limoDriveEuler);
            }

            if (!impactTriggered)
            {
                impactTimer += Time.deltaTime;

                Vector3 deltaToPlayer = controller.transform.position - limo.transform.position;
                deltaToPlayer.y = 0f;
                if (deltaToPlayer.magnitude <= killDistance)
                {
                    impactTriggered = true;
                    health.TakeDamage(health.CurrentHealth + health.maxHealth);
                }
            }
            else
            {
                postImpactTimer += Time.deltaTime;
                if (limoExitPoint == null || postImpactTimer >= postImpactDriveTime)
                {
                    controller.ClearCameraFollowOverride();
                    Destroy(cameraAnchor);
                    _globalSequenceStarted = false;
                    yield break;
                }

                Vector3 deltaToExit = limoExitPoint.position - limo.transform.position;
                deltaToExit.y = 0f;
                if (deltaToExit.magnitude <= 0.15f)
                {
                    controller.ClearCameraFollowOverride();
                    Destroy(cameraAnchor);
                    _globalSequenceStarted = false;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private GameObject CreateLimo()
    {
        if (sceneLimoObject == null)
        {
            GameObject fallbackSceneLimo = FindSceneLimoObject();
            if (fallbackSceneLimo != null)
            {
                sceneLimoObject = fallbackSceneLimo;
            }
        }

        if (sceneLimoObject != null)
        {
            sceneLimoObject.transform.position = GetLimoSpawnPosition();
            sceneLimoObject.SetActive(true);
            sceneLimoObject.transform.rotation = Quaternion.Euler(limoDriveEuler);
            sceneLimoObject.transform.localScale = limoScale;
            PrepareSceneLimoObject(true);
            ApplyMaterialOverride(sceneLimoObject);
            EnsureVisibleLimoScale(sceneLimoObject);

            return sceneLimoObject;
        }

        if (limoPrefab == null)
        {
            return null;
        }

        Vector3 position = GetLimoSpawnPosition();
        Quaternion rotation = Quaternion.Euler(limoDriveEuler);
        GameObject limo = Instantiate(limoPrefab, position, rotation);
        limo.transform.localScale = limoScale;
        ApplyMaterialOverride(limo);
        EnsureVisibleLimoScale(limo);
        return limo;
    }

    private Vector3 GetImpactDestination(UserImportedPlayerController controller)
    {
        if (playerImpactPoint != null)
        {
            Vector3 destination = playerImpactPoint.position;
            destination.x = controller.transform.position.x;
            return destination;
        }

        Vector3 fallback = controller.transform.position;
        fallback.z = transform.position.z + roadZOffset;
        return fallback;
    }

    private Vector3 GetLimoExitDestination(float driveLaneZ, Vector3 fallbackPosition)
    {
        if (limoExitPoint != null)
        {
            return new Vector3(limoExitPoint.position.x, fallbackPosition.y, driveLaneZ);
        }

        return fallbackPosition;
    }

    private Vector3 GetLimoSpawnPosition()
    {
        Vector3 position = limoSpawnPoint != null ? limoSpawnPoint.position : transform.position;
        position.y += limoSpawnYOffset;
        return position;
    }

    private void ApplyMaterialOverride(GameObject limo)
    {
        if (limo == null || limoMaterialOverride == null)
        {
            return;
        }

        foreach (Renderer renderer in limo.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = limoMaterialOverride;
        }
    }

    private void PrepareSceneLimoObject(bool visible)
    {
        if (sceneLimoObject == null)
        {
            return;
        }

        sceneLimoObject.SetActive(visible);
        foreach (Renderer renderer in sceneLimoObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }
    }

    private GameObject FindSceneLimoObject()
    {
        GameObject activeLimo = GameObject.Find("LimoPreview");
        if (activeLimo != null)
        {
            return activeLimo;
        }

        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "LimoPreview" && go.scene == gameObject.scene)
            {
                return go;
            }
        }

        return null;
    }

    private void EnsureVisibleLimoScale(GameObject limo)
    {
        if (limo == null || minimumLimoVisibleLength <= 0f)
        {
            return;
        }

        Bounds bounds = CalculateRendererBounds(limo);
        float longestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (longestSide <= 0.0001f || longestSide >= minimumLimoVisibleLength)
        {
            return;
        }

        float scaleMultiplier = minimumLimoVisibleLength / longestSide;
        limo.transform.localScale *= scaleMultiplier;
    }

    private Bounds CalculateRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
