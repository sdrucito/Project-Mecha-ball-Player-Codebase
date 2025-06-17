using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public class PlayerVFX : MonoBehaviour
    {
        private Material _actualFireMaterial;
        private float _lastHealthValue = 1.0f;
        [SerializeField] private Material damageMaterial;
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Renderer materialRenderer;
        [SerializeField] private int[] materialSlots;
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
            float lerpSpeed = 5f;
            Material[] materials = materialRenderer.materials;
            _actualFireMaterial = materials[materialSlot];
            Material targetMaterial = damageMaterial;
            
            materials[materialSlot] = new Material(_actualFireMaterial);
            
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(_actualFireMaterial, targetMaterial, t);
        
                // write the modified array back to the renderer
                materialRenderer.materials = materials;

                yield return null;
            }
            t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(targetMaterial, _actualFireMaterial, t);
        
                // write the modified array back to the renderer
                materialRenderer.materials = materials;

                yield return null;
            }
            materialRenderer.materials[materialSlot] = new Material(_actualFireMaterial);
            
        }

        // Make glow color change with the health percentage. If the health is under a given threshold
        // the glow lerps to its "damaged" version
        public void SetGlowColor(float healthPercentage)
        {
            if (healthPercentage > _lastHealthValue)
            {
                LerpGlowRecovery(healthPercentage);
            }
            else if(healthPercentage < 0.6f)
            {
                LerpGlowDamage(healthPercentage);
            }
            _lastHealthValue = healthPercentage;
        }

        private void LerpGlowRecovery(float healthPercentage)
        {
            float t = 1.0f;
            if(healthPercentage < 0.0f)
                t = 1.0f - (healthPercentage / 0.5f) + 0.2f;
            foreach (var materialSlot in materialSlots)
            {
                Material[] materials = materialRenderer.materials;
                _actualFireMaterial = materials[materialSlot];
                Material targetMaterial = baseMaterial;
            
                if (!materials[materialSlot].name.Contains("Instance"))
                {
                    materials[materialSlot] = new Material(materials[materialSlot]);
                }
                materials[materialSlot].Lerp(_actualFireMaterial, targetMaterial, t);

                materialRenderer.materials = materials;
            }
        }
        private void LerpGlowDamage(float healthPercentage)
        {
            float t = 1.0f - (healthPercentage / 0.5f) + 0.2f;
            foreach (var materialSlot in materialSlots)
            {
                Material[] materials = materialRenderer.materials;
                _actualFireMaterial = materials[materialSlot];
                Material targetMaterial = damageMaterial;
            
                if (!materials[materialSlot].name.Contains("Instance"))
                {
                    materials[materialSlot] = new Material(materials[materialSlot]);
                }
                materials[materialSlot].Lerp(_actualFireMaterial, targetMaterial, t);

                materialRenderer.materials = materials;
                    
            }
        }
    }
}
