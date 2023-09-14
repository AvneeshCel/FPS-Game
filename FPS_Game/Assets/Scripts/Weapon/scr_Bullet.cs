using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class scr_Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float lifeTime = 1f;
    public Vector3 force;
    public Rigidbody rb;
    public LayerMask enemiesLayer;

    [Range(0f, 1f)]
    public float bounciness;
    public bool useGravity;

    public int explosionDamage;
    public int explosionRange;

    public int maxCollisions;
    public float maxLifetimes;
    public bool explodeOnTouch = true;

    int collisions;


    private Action<scr_Bullet> _destroyAction;

    private void Awake()
    {
        //Destroy(gameObject, lifeTime);
        rb = GetComponent<Rigidbody>();
        rb.useGravity = useGravity;

    }

    private void Start()
    {
       // rb.AddForce(force, ForceMode.Impulse);
        //StartCoroutine(DestroyIt());
    }

    private void OnEnable()
    {
       // rb.AddForce(transform.forward * 19f, ForceMode.Impulse);
        StartCoroutine(DestroyIt());

    }

    private void OnDisable()
    {
        rb.velocity = Vector3.zero;
    }

    private void Explode()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, enemiesLayer);

        for (int i = 0; i < enemies.Length; i++)
        {
           // enemies[i].GetComponent
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //m_pool?.Release(this);
        //if (collision.collider.CompareTag("Enemy") && explodeOnTouch) Explode();
    }

    IEnumerator DestroyIt()
    {
        yield return new WaitForSeconds(4f);
        _destroyAction(this);
      
    }

    public void Init(Action<scr_Bullet> destroyAction)
    {
        _destroyAction = destroyAction;
    }
}
