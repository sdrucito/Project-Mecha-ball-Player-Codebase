using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public class PlayerVFX : MonoBehaviour
    {
        [Header("Materials Renderer")]
        private Material _currentFireMaterial;
        private float _lastHealthValue = 1.0f;
        [SerializeField] private Material damageMaterial;
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Renderer materialRenderer;
        [SerializeField] private int[] materialSlots;
        [Header("Trail Renderer")]
        [SerializeField] private float minTrailVelocity;
        [SerializeField] private float maxTrailVelocity;
        [SerializeField] private float minTrailTime;
        [SerializeField] private float maxTrailTime;
        [SerializeField] float fadeDuration = 0.5f;
        private TrailRenderer _trailRenderer;

        private bool canDamage = true;
        void Start()
        {
            Player.Instance.PawnAttributes.OnHealthChange += SetGlowColor;
            _trailRenderer = GetComponent<TrailRenderer>();
            _trailRenderer.time = 0.0f;
        }

        public void TakeDamage()
        {
            foreach (int materialSlot in materialSlots)
            {
                StartCoroutine(TakeDamageCoroutine(materialSlot));
            }
        }

        private IEnumerator TakeDamageCoroutine(int materialSlot)
        {
            // Wait until we're allowed to damage
            yield return new WaitUntil(() => canDamage);

            var mats = materialRenderer.materials;
            if (materialSlot < 0 || materialSlot >= mats.Length)
            {
                Debug.LogError($"Invalid material slot {materialSlot}");
                yield break;
            }

            var mat = mats[materialSlot];
            mat.EnableKeyword("_EMISSION");
            var originalColor = mat.GetColor("_EmissionColor");
            var targetColor  = damageMaterial.GetColor("_EmissionColor");

            float duration = 0.2f;
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                mat.SetColor("_EmissionColor", Color.Lerp(originalColor, targetColor, t));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                mat.SetColor("_EmissionColor", Color.Lerp(targetColor, originalColor, t));
                yield return null;
            }

            mat.SetColor("_EmissionColor", originalColor);
            
        }

        // Make glow color change with the health percentage. If the health is under a given threshold
        // the glow lerps to its "damaged" version
        public void SetGlowColor(float healthPercentage)
        {
            
            canDamage = false;
            
            if (healthPercentage > _lastHealthValue)
            {
                LerpGlowRecovery(healthPercentage);
            }
            else if(healthPercentage < 0.6f)
            {
                LerpGlowDamage(healthPercentage);
            }
            _lastHealthValue = healthPercentage;
            canDamage = true;
            
        }

        private void LerpGlowRecovery(float healthPercentage)
        {
            float t = 1.0f;
            if(healthPercentage < 0.0f)
                t = 1.0f - (healthPercentage / 0.5f) + 0.2f;
            foreach (var materialSlot in materialSlots)
            {
                Material[] materials = materialRenderer.materials;
                _currentFireMaterial = materials[materialSlot];
                Material targetMaterial = baseMaterial;
            
                if (!materials[materialSlot].name.Contains("Instance"))
                {
                    materials[materialSlot] = new Material(materials[materialSlot]);
                }
                materials[materialSlot].Lerp(_currentFireMaterial, targetMaterial, t);

                materialRenderer.materials = materials;
            }
        }
        private void LerpGlowDamage(float healthPercentage)
        {
            float t = 1.0f - (healthPercentage / 0.5f) + 0.2f;
            foreach (var materialSlot in materialSlots)
            {
                Material[] materials = materialRenderer.materials;
                _currentFireMaterial = materials[materialSlot];
                Material targetMaterial = damageMaterial;
            
                if (!materials[materialSlot].name.Contains("Instance"))
                {
                    materials[materialSlot] = new Material(materials[materialSlot]);
                }
                materials[materialSlot].Lerp(_currentFireMaterial, targetMaterial, t);

                materialRenderer.materials = materials;
                    
            }
        }

        public void ResetTrails()
        {
            _trailRenderer.emitting = false;
        }
        public void UpdateTrailRenders(float velocity)
        {
            if (velocity > minTrailVelocity && velocity <= maxTrailVelocity)
            {
                _trailRenderer.emitting = true;
                float t = Mathf.InverseLerp(minTrailVelocity, maxTrailVelocity, velocity);
                _trailRenderer.time = Mathf.Lerp(minTrailTime, maxTrailTime, t);
            }else if (velocity > maxTrailVelocity)
            {
                _trailRenderer.emitting = false;
                _trailRenderer.time = 0.0f;
            }
            else
            {
                float fadeSpeed = maxTrailTime / fadeDuration;
                _trailRenderer.time = Mathf.MoveTowards(_trailRenderer.time, 0.0f, fadeSpeed*Time.fixedDeltaTime);
            }
        }
    }
}
