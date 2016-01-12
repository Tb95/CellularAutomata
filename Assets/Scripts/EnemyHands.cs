using UnityEngine;
using System.Collections;

public class EnemyHands : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
            Debug.Log("Hit player");
    }
}
