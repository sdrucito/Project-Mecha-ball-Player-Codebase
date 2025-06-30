using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public class PlayerVFX : MonoBehaviour
    {
        private Material _currentFireMaterial;
        private float _lastHealthValue = 1.0f;
        [SerializeField] private Material damageMaterial;
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Renderer materialRenderer;
        [SerializeField] private int[] materialSlots;
        
        private bool canDamage = true;
        void Start()
        {
            Player.Instance.PawnAttributes.OnHealthChange += SetGlowColor;
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
    }
}
