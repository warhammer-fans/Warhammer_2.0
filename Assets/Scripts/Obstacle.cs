using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Obstacle : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject); // nie niszczyć podczas zmiany sceny
    }
    void OnMouseOver()
    {
        // Jeżeli nie jesteśmy w kreatorze pola bitwy to funkcja stawiania przeszkód jest wyłączona
        if (SceneManager.GetActiveScene().buildIndex != 1 || GameManager.PanelIsOpen || GameObject.Find("ObstaclesDropdown").GetComponent<TMP_Dropdown>().IsExpanded)
            return;

        Vector3 mousePosition = new Vector3(Mathf.Round(Camera.main.ScreenToWorldPoint(Input.mousePosition).x), Mathf.Round(Camera.main.ScreenToWorldPoint(Input.mousePosition).y), Mathf.Round(Camera.main.ScreenToWorldPoint(Input.mousePosition).z));

        Collider2D collider = Physics2D.OverlapCircle(mousePosition, 0.1f);

        if (collider != null && GridManager.obstacleRemoving && (collider.gameObject.tag == "Tree" || collider.gameObject.tag == "Rock" || collider.gameObject.tag == "Wall" || collider.gameObject.tag == "Water") && Tile.isMouseDragging)
        {
            Destroy(collider.gameObject);
        }
    }
}
