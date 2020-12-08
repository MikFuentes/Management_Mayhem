using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class NPC_Script : NetworkBehaviour
{
    public Sprite[] item_sprite_array = null;
    public GameObject sprite_go;

    private void Start()
    {
        item_sprite_array = Resources.LoadAll<Sprite>("Val's_Lovely_Art/Pickups/Items");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        //switch (rand)
        //{
        //    case 0:
        //        if (collision.gameObject.name == "Rendang")
        //        {
        //            GetRendang();
        //        }
        //        //RecipeObject.Artwork.SetActive(false);
        //        break;

        //    case 1:
        //        //CuisineObject.Artwork.SetActive(true);
        //        if (collision.gameObject.name == "Gado Gado")
        //        {
        //            GetGadoGado();
        //        }

        //        //RecipeObject.Artwork.SetActive(false);
        //        break;

        //    case 2:
        //        if (collision.gameObject.name == "Soto")
        //        {
        //            GetSoto();
        //        }

        //        //CuisineObject.Artwork.SetActive(false);
        //        break;
        //}
    }

    [ClientRpc]
    public void RpcChangeSprite(int rand)
    {
        sprite_go.GetComponent<SpriteRenderer>().sprite = item_sprite_array[rand]; // assign random sprite from array
    }
}
