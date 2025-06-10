using System.Collections;
using UnityEngine;

namespace Player
{
    public class PlayerVFX : MonoBehaviour
    {
        private Material _actualFireMaterial;
        [SerializeField] private Material damageMaterial;

        [SerializeField] private Renderer renderer;
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
            Debug.Log("Starting damage lerp");
            float lerpSpeed = 5f;
            Material[] materials = renderer.materials;
            _actualFireMaterial = materials[materialSlot];
            Material targetMaterial = damageMaterial;
            
            materials[materialSlot] = new Material(_actualFireMaterial);
            
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(_actualFireMaterial, targetMaterial, t);
        
                // write the modified array back to the renderer
                renderer.materials = materials;

                yield return null;
            }
            t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(targetMaterial, _actualFireMaterial, t);
        
                // write the modified array back to the renderer
                renderer.materials = materials;

                yield return null;
            }
            renderer.materials[materialSlot] = new Material(_actualFireMaterial);
            Debug.Log("Ending damage lerp");

            
        }

        // Make glow color change with the health percentage. If the health is under a given threshold
        // the glow lerps to its "damaged" version
        public void SetGlowColor(float healtPercentage)
        {
            if (healtPercentage < 0.5f)
            {
                float t = 1.0f - (healtPercentage / 0.5f) + 0.2f;
                foreach (var materialSlot in materialSlots)
                {
                    Material[] materials = renderer.materials;
                    _actualFireMaterial = materials[materialSlot];
                    Material targetMaterial = damageMaterial;
            
                    if (!materials[materialSlot].name.Contains("Instance"))
                    {
                        materials[materialSlot] = new Material(materials[materialSlot]);
                    }
                    materials[materialSlot].Lerp(_actualFireMaterial, targetMaterial, t);

                    renderer.materials = materials;
                    
                }
                
            }
        }
    }
}
