using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellCollision : MonoBehaviour
{
    public GameObject explosionParticlesPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        // Destroy the shell if it hits something (e.g. rock, ground, enemytank, etc.). 
        // If the shell hits enemy tank, the enemy tank will also be destroyed.
        if (explosionParticlesPrefab){
            GameObject explosion = (GameObject)Instantiate(explosionParticlesPrefab, transform.position, explosionParticlesPrefab.transform.rotation);
            Destroy(explosion, explosion.GetComponent<ParticleSystem>().main.startLifetimeMultiplier);
            Destroy(gameObject);
        }

        if(collision.gameObject.tag == "Tank")
        {
            Destroy(collision.gameObject);
        }

    }

}
