using UnityEngine;
using System.Collections;

public class EnemyHands : MonoBehaviour {

    int damage;

    void Start()
    {
        damage = GetComponentInParent<EnemyController>().damage;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            other.GetComponent<PlayerHealth>().Hit(damage);
        }
    }
}
