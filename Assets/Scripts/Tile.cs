using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour
{
    public Color baseColor, offsetColor, highlightColor, rangeColor, rangeHighlightColor;
    [HideInInspector] public Renderer _renderer;
    [HideInInspector] public Color normalColor;


    public bool isOccupied;
    private LayerMask layer;

    public static bool isMouseDragging; // Określa, czy lewy przycisk myszy jest przytrzymany. Wtedy można stawiać przeszkody bez konieczności klikania za każdym razem

    public void Init(bool isOffset)
    {
        _renderer.material.color = isOffset ? offsetColor : baseColor;
        normalColor = _renderer.material.color;
        layer = LayerMask.GetMask("Character");

        // Ustala kolor podswietlonych pol na taki sam jak normalny ale z opacity na poziomie 80%
        rangeColor = _renderer.material.color * 0.92f;
        rangeHighlightColor = highlightColor * 0.9f;
    }

    void Update()
    {
        if(!MovementManager.isMoving)
        {
            // Sprawdza czy na polu stoi jakas postac
            Collider2D collider = Physics2D.OverlapCircle(transform.position, 0.1f, layer);
            isOccupied = (collider != null) ? true : false;
        }

        if (Input.GetMouseButtonDown(0)) // Sprawdza, czy lewy przycisk myszy został puszczony
            isMouseDragging = true;
        else if (Input.GetMouseButtonUp(0))
            isMouseDragging = false;
    }

    void OnMouseEnter()
    {
        //podswietlenie pola
        if (MovementManager.canMove == true)
        {
            // Jezeli alpha pola jest rowne 1 (czyli nie jest to pole w zasiegu ruchu) to ustala standardowy kolor podswietlenia, w przeciwnym razie ustala rangeHighlightColor
            if (_renderer.material.color.a == 1f)
                _renderer.material.color = highlightColor;
            else
                _renderer.material.color = rangeHighlightColor;
        }
        if (MagicManager.targetSelecting && Character.selectedCharacter.GetComponent<Stats>().AreaSize > 0)
        {
            GameObject.Find("MagicManager").GetComponent<MagicManager>().HighlightTilesInSpellRange(this.gameObject);
        }
    }

    void OnMouseExit()
    {
        // Przywrócenie normalnego koloru, ale tylko jeśli obecny kolor nie jest równy rangeHighlightColor
        if (_renderer.material.color != rangeHighlightColor && _renderer.material.color == highlightColor)
            _renderer.material.color = normalColor;
        else if (MovementManager.canMove == true)
            _renderer.material.color = rangeColor;
    }

    void OnMouseUp()
    {
        // Jeżeli jesteśmy w kreatorze pola bitwy to funkcja OnMouseUp jest nieaktywna
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (GameManager.PanelIsOpen)
                return;

            // Zapobiega przypadkowemu postawieniu przeszkody podczas wybierania opcji dropdownu
            if (GameObject.Find("ObstaclesDropdown").GetComponent<TMP_Dropdown>().IsExpanded)
                return;      

            return;
        }

        if (CharacterManager.characterAdding && SceneManager.GetActiveScene().buildIndex == 2)
        {
            GameObject.Find("CharacterManager").GetComponent<CharacterManager>().CreateNewCharacter(CharacterManager.characterTag, "", new Vector2 (this.transform.position.x, this.transform.position.y));
            CharacterManager.characterAdding = false;
            return;
        }

        if (MagicManager.targetSelecting == true && !GameManager.PanelIsOpen)
        {
            if (Character.selectedCharacter.GetComponent<Stats>().OffensiveSpell)
                GameObject.Find("MagicManager").GetComponent<MagicManager>().GetMagicDamage(this.gameObject);
            else
                GameObject.Find("MagicManager").GetComponent<MagicManager>().HealingSpell(this.gameObject);

            GameObject.Find("MagicManager").GetComponent<MagicManager>().ResetHighlightTilesInSpellRange();

            // Resetuje tryb wyboru celu ataku
            AttackManager.targetSelecting = false;
            MagicManager.targetSelecting = false;

            Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(Character.selectedCharacter);
            Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(Character.selectedCharacter);
        }

        MovementManager movementManager = GameObject.Find("MovementManager").GetComponent<MovementManager>();

        // Ustala jaka postac ma sie ruszyc
        GameObject character = Character.selectedCharacter;

        // wywoluje akcje ruchu wewnatrz klasy MovementManager
        if(character != null && !GameManager.PanelIsOpen && MovementManager.canMove && !isOccupied)
            movementManager.MoveSelectedCharacter(this.gameObject, character);    
        else if (isOccupied)
        {
            Debug.Log("Wybrane pole jest zajęte.");
        }
    }

    void OnMouseOver()
    {
        // Jeżeli nie jesteśmy w kreatorze pola bitwy to funkcja stawiania przeszkód jest wyłączona
        if (SceneManager.GetActiveScene().buildIndex != 1 || GameManager.PanelIsOpen || GameObject.Find("ObstaclesDropdown").GetComponent<TMP_Dropdown>().IsExpanded)
            return;

        // sprawdź, czy wskaźnik nie znajduje się nad GUI; jeśli tak, wróć
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mousePosition = new Vector3(Mathf.Round(Camera.main.ScreenToWorldPoint(Input.mousePosition).x), Mathf.Round(Camera.main.ScreenToWorldPoint(Input.mousePosition).y), Mathf.Round(Camera.main.ScreenToWorldPoint(Input.mousePosition).z));

        Collider2D collider = Physics2D.OverlapCircle(mousePosition, 0.1f);

        if (collider != null && collider.gameObject.tag == "Tile" && isMouseDragging)
        {
            if (GridManager.treeAdding)
            {
                GameObject.Find("Grid").GetComponent<GridManager>().AddObstacle(mousePosition, "Tree", false);
            }
            else if (GridManager.rockAdding)
            {
                GameObject.Find("Grid").GetComponent<GridManager>().AddObstacle(mousePosition, "Rock", false);
            }
            else if (GridManager.wallAdding)
            {
                GameObject.Find("Grid").GetComponent<GridManager>().AddObstacle(mousePosition, "Wall", false);
            }
            else if (GridManager.waterAdding)
            {
                GameObject.Find("Grid").GetComponent<GridManager>().AddObstacle(mousePosition, "Water", false);
            }
        }
    }
}