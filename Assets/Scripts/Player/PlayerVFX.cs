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
        
        private bool canDamage = false;
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
            while (!canDamage)
            {
                yield return null;
            }
            float lerpSpeed = 5f;
            Material[] materials = materialRenderer.materials;
            Material original = materials[materialSlot];
            Material target = damageMaterial;
    
            materials[materialSlot] = new Material(original);
            materialRenderer.materials = materials;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(original, target, t);
                materialRenderer.materials = materials;       
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(target, original, t);
                materialRenderer.materials = materials;
                yield return null;
            }
            materials[materialSlot] = new Material(original);
            materialRenderer.materials = materials;
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
