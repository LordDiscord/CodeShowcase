using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class MainCharacter : Character
{

    public static MainCharacter instance; // Singleton instance

    private int count = 0;
    private bool fighter = true; // Esto es la clase default para esta prueba, luego se podrán escoger

    public Dictionary<string, List<Item>> inventory = new Dictionary<string, List<Item>>();

    protected virtual void Awake()
    {
        // Implementación del Singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // No destruir al cambiar de escena
        }
        else
        {
            Destroy(gameObject); // Destruir duplicados
            return;
        }
        inventory = new Dictionary<string, List<Item>>();
    }

    protected virtual void Start()
    {
        if (instance == this) // Solo inicializar si este objeto es la instancia
        {
            GenerateNewStats();
            currentHealth = health;
            StartCoroutine(IsDead());
        }
    }

    IEnumerator IsDead()
    {
        while (true)
        {
            if (health <= 0)
            {
                popUpText.text = (currentHealth - health).ToString();
                Vector3 newPosition = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
                Instantiate(popUpDamagePrefab, newPosition, Quaternion.identity); anim.SetBool("Dead", true);
                Debug.Log("HAS MUERTO");
                yield return new WaitForSeconds(2f);
                Destroy(gameObject);
                yield break;
            }
            else
            {
                anim.SetBool("Dead", false);
                if (health < currentHealth)
                {
                    popUpText.text = (currentHealth - health).ToString();
                    Vector3 newPosition = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
                    Instantiate(popUpDamagePrefab, newPosition, Quaternion.identity); anim.SetBool("IsDamaged", true);
                    yield return new WaitForSeconds(0.2f);
                    anim.SetBool("IsDamaged", false);
                    currentHealth = health;
                }
            }
            yield return null;
        }
    }
    public void GenerateNewStats()
    {
        // Inicializa las estadísticas básicas
        attacks = 0;
        health = Random.Range(15, 21);
        armor = 10;
        dexterity = 10;
        intelligence = 10;
        speed = 6;
        initiative = dexterity / 2 + throwD6(); // Posible cambio (6~12)
        name = "player";
        if (fighter)
        {
            attacks = 1;
            armor += 3;
            dexterity += throwD4() + 2;
            intelligence -= 4;
            health += 7;
            initiative -= 3;
        }
        currentHealth = health;
        Debug.Log("GENERANDO");
    }

    public void AddItem(Item item)
    {
        if (!inventory.ContainsKey(item.Type))
        {
            inventory[item.Type] = new List<Item>();
        }
        inventory[item.Type].Add(item);
    }


    // Método para usar un ítem del inventario
    public void UseItem(string itemType)
    {
        if (inventory.ContainsKey(itemType) && inventory[itemType].Count > 0)
        {
            // Aquí puedes implementar la lógica para usar el ítem del tipo especificado
            Item item = inventory[itemType][0]; // Tomamos el primer ítem del tipo especificado
            if (itemType == "HealthPotion")
            {
                health += ((HealthPotion)item).HealAmount;
            }
            else if (itemType == "ArmorPotion")
            {
                armor += ((ArmorPotion)item).ArmorAmount;
            }
            else if (itemType == "SpeedPotion")
            {
                speed += ((SpeedPotion)item).SpeedAmount;
            }
            else if (itemType == "AttackPotion")
            {
                attacks += ((AttackPotion)item).AttackAmount;
            }
            inventory[itemType].RemoveAt(0); // Eliminamos el ítem del inventario
            Debug.Log(item.Name + " usado.");
        }
        else
        {
            Debug.Log("No hay ítems del tipo " + itemType + " en el inventario.");
        }
    }

    public int GetItemCount(string itemKey)
    {
        if (inventory.ContainsKey(itemKey))
        {
            count = inventory[itemKey].Count;
        }
        else
        {
            count = 0;
        }
        Debug.Log(itemKey+": "+count);
        return count;
    }

    public void RespawnAt(Vector3 position)
    {
        transform.position = position;
    }

    public void AttackUp() // activar animaciones
    {
        anim.SetBool("AttackUp", true);
    }
    public void NoAttackUp()
    {
        anim.SetBool("AttackUp", false);
    }
    public void AttackDown()
    {
        anim.SetBool("AttackDown", true);
    }
    public void NoAttackDown()
    {
        anim.SetBool("AttackDown", false);
    }
    public void AttackRight()
    {
        anim.SetBool("AttackRight", true);
    }
    public void NoAttackRight()
    {
        anim.SetBool("AttackRight", false);
    }
}