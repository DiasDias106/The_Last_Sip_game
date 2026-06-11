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

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip crashSound;
    [SerializeField] private float engineVolume = 0.8f;
    [SerializeField] private float crashVolume = 0.5f;
    [SerializeField] private bool loopEngineSound = false;
    [SerializeField, Range(0.1f, 3f)] private float enginePitch = 1.0f;

    [Header("Sequence")]
    [SerializeField] private float playerWalkSpeed = 3.75f;
    [SerializeField] private float limoSpeed = 13f;
    [SerializeField] private float killDistance = 4.5f;
    [SerializeField] private float impactPointDistance = 1.6f;
    [SerializeField] private float limoStartDelay = 0.15f;
    [SerializeField] private float preImpactLookDelay = 0.35f;
    [SerializeField] private float limoImpactTimeout = 4f;
    [SerializeField] private float maxAutoWalkTime = 2.5f;
    [SerializeField] private float postImpactDriveTime = 2.5f;
    [SerializeField] private float cameraPitch = 16f;
    [SerializeField] private float minimumLimoVisibleLength = 7f;

    private static bool _globalSequenceStarted;
    private bool _sequenceStarted;

    private void Awake()
    {
        _globalSequenceStarted = false;
        _sequenceStarted = false;

        // Force kill distance to be larger so the collision happens at the front bumper
        if (killDistance < 4.0f) killDistance = 4.5f;

        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger != null) trigger.isTrigger = true;

        // Impedir que a câmara do jogador bata nesta caixa invisível (evita o "salto" da câmara)
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

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

        CharacterController cc = controller.GetComponent<CharacterController>();
        float originalStepOffset = 0f;
        if (cc != null) { originalStepOffset = cc.stepOffset; }

        Vector3 targetPosition = GetImpactDestination(controller);

        // Camera anchor that follows the player during the walk so the user can see it
        Vector3 toTarget = targetPosition - controller.transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) toTarget = controller.transform.forward;
        else toTarget.Normalize();

        GameObject walkCameraAnchor = new GameObject("NightWalkCameraAnchor");
        walkCameraAnchor.transform.SetParent(controller.transform);
        walkCameraAnchor.transform.localPosition = Vector3.zero;
        walkCameraAnchor.transform.rotation = Quaternion.LookRotation(toTarget);
        controller.SetCameraFollowOverride(walkCameraAnchor.transform, true, cameraPitch, snapImmediately: true);

        float walkSpeed = controller.CurrentWalkSpeed;
        float autoWalkTimer = 0f;
        while (Mathf.Abs(controller.transform.position.z - targetPosition.z) > 0.12f ||
               Mathf.Abs(controller.transform.position.x - targetPosition.x) > 0.12f)
        {
            Vector3 direction = targetPosition - controller.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                controller.FaceDirection(direction);
                controller.MoveExternally(direction.normalized * walkSpeed);
            }

            walkCameraAnchor.transform.rotation = Quaternion.LookRotation(toTarget);

            autoWalkTimer += Time.deltaTime;
            if (autoWalkTimer >= maxAutoWalkTime)
            {
                break;
            }

            yield return null;
        }

        controller.ClearCameraFollowOverride();
        Destroy(walkCameraAnchor);

        if (cc != null) { cc.stepOffset = originalStepOffset; }
        controller.FaceDirection(Vector3.forward);
        controller.PlayExternalAnimation("idle");
        yield return new WaitForSeconds(limoStartDelay);

        GameObject limo = CreateLimo();
        if (limo == null)
        {
            health.TakeDamage(health.CurrentHealth + health.maxHealth);
            yield break;
        }

        // Make limo colliders triggers so the player's CharacterController doesn't step onto the car
        foreach (Collider col in limo.GetComponentsInChildren<Collider>(true))
            col.isTrigger = true;

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

        Vector3 initialDestination = new Vector3(controller.transform.position.x, limo.transform.position.y, driveLaneZ);
        Vector3 initialDirection = initialDestination - limo.transform.position;
        initialDirection.y = 0f;
        SetupHeadlights(limo, initialDirection);

        AudioSource limoAudio = null;
        if (engineSound != null || crashSound != null)
        {
            limoAudio = limo.AddComponent<AudioSource>();
            limoAudio.spatialBlend = 1f; // Som 3D
            limoAudio.minDistance = 5f;
            limoAudio.maxDistance = 50f;
            
            if (engineSound != null)
            {
                limoAudio.clip = engineSound;
                limoAudio.volume = engineVolume;
                limoAudio.loop = loopEngineSound;
                limoAudio.pitch = enginePitch;
                limoAudio.Play();
            }
        }

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

                if (_headlightsGroup != null)
                {
                    _headlightsGroup.transform.position += step;
                    _headlightsGroup.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            if (!impactTriggered)
            {
                impactTimer += Time.deltaTime;

                Vector3 deltaToPlayer = controller.transform.position - limo.transform.position;
                deltaToPlayer.y = 0f;
                if (deltaToPlayer.magnitude <= killDistance)
                {
                    impactTriggered = true;
                    if (limoAudio != null && crashSound != null)
                    {
                        limoAudio.PlayOneShot(crashSound, crashVolume);
                    }
                    
                    PlayerDeathSceneLoader loader = health.GetComponent<PlayerDeathSceneLoader>();
                    if (loader != null) loader.DeathScreenDelay = 0f;
                    
                    health.TakeDamage(health.CurrentHealth + health.maxHealth);
                }
                else if (impactTimer >= limoImpactTimeout)
                {
                    controller.ClearCameraFollowOverride();
                    Destroy(cameraAnchor);
                    if (_headlightsGroup != null) Destroy(_headlightsGroup);
                    yield break;
                }
            }
            else
            {
                postImpactTimer += Time.deltaTime;
                if (postImpactTimer >= postImpactDriveTime)
                {
                    controller.ClearCameraFollowOverride();
                    Destroy(cameraAnchor);
                    if (_headlightsGroup != null) Destroy(_headlightsGroup);
                    yield break;
                }
            }

            yield return null;
        }
    }

    private GameObject _headlightsGroup;

    private void SetupHeadlights(GameObject limo, Vector3 direction)
    {
        if (_headlightsGroup != null) Destroy(_headlightsGroup);
        _headlightsGroup = new GameObject("LimoHeadlights");

        Bounds bounds = CalculateRendererBounds(limo);
        _headlightsGroup.transform.position = bounds.center;

        if (direction.sqrMagnitude > 0.001f)
        {
            _headlightsGroup.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        float length = Mathf.Max(bounds.size.x, bounds.size.z);
        float width = Mathf.Min(bounds.size.x, bounds.size.z);

        if (length < 1f) length = 5f;
        if (width < 1f) width = 2.5f;

        // Ao usar 0.55f (em vez de 0.45f), garantimos que os faróis nascem ligeiramente
        // à frente do para-choques, fora da "caixa" do carro. Assim o próprio carro
        // não bloqueia a luz nem projeta sombras bizarras na estrada!
        float forwardOffset = length * 0.55f;
        float sideOffset = width * 0.4f; 
        
        // Subir os faróis um bocadinho mais para a grelha frontal
        float yOffset = -bounds.extents.y + 0.85f;

        CreateSingleLight("LeftLight", new Vector3(-sideOffset, yOffset, forwardOffset));
        CreateSingleLight("RightLight", new Vector3(sideOffset, yOffset, forwardOffset));
    }

    private void CreateSingleLight(string name, Vector3 localPosition)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(_headlightsGroup.transform, false);
        lightObj.transform.localPosition = localPosition;
        
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = 300f; // Alcança o fundo do beco
        l.spotAngle = 50f; // Mais focado (como "máximos" verdadeiros)
        l.innerSpotAngle = 25f; // Núcleo central focado
        l.intensity = 200f; // Intensidade extrema para estourar a câmara
        l.color = new Color(1f, 0.98f, 0.9f);
        l.shadows = LightShadows.Hard;
        
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(lightObj.transform, false);
        Light glow = glowObj.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 25f; // Área de "explosão" de luz à volta do carro
        glow.intensity = 60f; // Brilho absurdamente forte
        glow.color = l.color;
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
            return playerImpactPoint.position; // Vai exatamente para o centro/ponto definido
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
