using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class KunaiCreateObject : MonoBehaviour
{

    public GameObject prefab;
    private bool hasUsed = false;
    public ProjectileController parentScript;

    public void OnCollisionEnter(Collision other)
    {
        if (hasUsed) return;
        if(other.gameObject.tag == "Ground")
        {
            // Get face
            Vector3 normal = other.contacts[0].normal;
            GameObject pre = Instantiate(prefab, transform.position, Quaternion.FromToRotation(transform.up, normal));
            parentScript.generated_structures.Enqueue(pre.transform);
            if(parentScript.generated_structures.Count > 3)
            {
                Transform s = parentScript.generated_structures.Dequeue();
                Destroy(s.gameObject);
            }
        }
        hasUsed = true;
        
    }


}
