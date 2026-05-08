using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("»ù±¾ÊôÐÔ")]
    public int damage;
    public float attackRanage;  //¹¥»÷·¶Î§
    public float attackRate;    //¹¥»÷ÆµÂÊ

    private void OnTriggerStay2D(Collider2D other)
    {
        other.GetComponent<Character>()?.TakeDamage(this);
    }
}
