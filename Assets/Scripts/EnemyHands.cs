using UnityEngine;
using System.Collections;

public class EnemyHands : MonoBehaviour {

    int damage;
    EnemyController enemy;

    void Start()
    {
        enemy = GetComponentInParent<EnemyController>();
        damage = enemy.damage;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && enemy.IsAttacking())
        {
            other.GetComponent<PlayerHealth>().Hit(damage);
        }
    }
}
