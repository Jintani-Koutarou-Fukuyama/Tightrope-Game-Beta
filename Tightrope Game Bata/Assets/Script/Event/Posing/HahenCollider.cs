using UnityEngine;

public class HahenCollider : MonoBehaviour
{
    GameObject player;
    Damage damage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("SuitMan");
        damage=player.GetComponent<Damage>();
       // Invoke(nameof(DamageFlag), 13f);
        Invoke(nameof(Destroy), 15f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider t)
    {
        
        if (t.gameObject.tag=="Player")
        {
            Debug.Log("PlayerÇ…ìñÇΩÇ¡ÇΩ");
            damage.EventDamage();
            damage.DamageFlag = true;
        }
        if (t.gameObject.tag =="Explosion")
        {
            Debug.Log("ExplosionPointÇ…ìñÇΩÇ¡ÇΩ");
            damage.Explo();
            damage.NoDamage();
        }
    }
   // void DamageFlag()
   // {
    //    damage.NoDamage();//îjï–Ç™ÉvÉåÉCÉÑÅ[Ç…ìñÇΩÇÁÇ»Ç©Ç¡ÇΩÇÁ
   // }
    private void Destroy()
    {
        Destroy(this.gameObject);
    }
}
