using System.Collections;
using UnityEngine;

namespace Player
{
    public class PlayerVFX : MonoBehaviour
    {
        [SerializeField] private Material fireMaterial;
        [SerializeField] private Material damageMaterial;

        [SerializeField] private Renderer[] renderers;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Player.Instance.PawnAttributes.OnHealthChange += SetGlowColor;
        }

        public void TakeDamage()
        {
            foreach (var renderer in renderers)
            {
                StartCoroutine(TakeDamageCoroutine(renderer));
            }
        }

        private IEnumerator TakeDamageCoroutine(Renderer renderer)
        {
            int materialSlot = 0;
            float lerpSpeed = 2.5f;
            Material[] materials = renderer.materials;
            Material startMaterial = materials[materialSlot];
            Material targetMaterial = damageMaterial;
            
            materials[materialSlot] = new Material(startMaterial);
            
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(startMaterial, targetMaterial, t);
        
                // write the modified array back to the renderer
                renderer.materials = materials;

                yield return null;
            }
            
            
            t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * lerpSpeed;
                materials[materialSlot].Lerp(targetMaterial, startMaterial, t);
        
                // write the modified array back to the renderer
                renderer.materials = materials;

                yield return null;
            }
            renderer.materials[materialSlot] = new Material(fireMaterial);
        }

        public void SetGlowColor(float health)
        {
            
        }
    }
}
