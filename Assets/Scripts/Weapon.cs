using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {

    [Range(1, 100)]
    public int damage;

    Player3D player;

    void Start()
    {
        player = GetComponentInParent<Player3D>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Enemy" && !other.isTrigger && player.IsAttacking())
        {
            other.GetComponent<EnemyController>().Hit(damage);
        }
    }
}
