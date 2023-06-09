using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MovementManager : MonoBehaviour
{
    public static bool canMove; // okresla czy postac moze wykonac ruch
    public static bool Charge; // szarza
    public static bool Run; //bieg
    public static bool isMoving; // okresla, ze postac jest trakcie animacji ruchu

    // Lista wszystkich pol w zasiegu ruchu postaci
    [HideInInspector] public List<GameObject> tilesInMovementRange = new List<GameObject>();

    // Lista wszystkich pol na ścieżce ruchu postaci
    [HideInInspector] public List<GameObject> tilesOnPath = new List<GameObject>();

    private GridManager grid;

    private CharacterManager characterManager;
    private MessageManager messageManager;

    void Start()
    {
        canMove = false;
        isMoving = false;
        grid = GameObject.Find("Grid").GetComponent<GridManager>();

        // Odniesienie do Menadzera Wiadomosci wyswietlanych na ekranie gry
        messageManager = GameObject.Find("MessageManager").GetComponent<MessageManager>();

        // Odniesienie do menadżera postaci
        characterManager = GameObject.Find("CharacterManager").GetComponent<CharacterManager>();
    }

    public void ResetChargeAndRun()
    {
        Charge = false;
        Run = false;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        GameObject[] characters = enemies.Concat(players).ToArray();

        foreach(var character in characters) character.GetComponent<Stats>().tempSz = character.GetComponent<Stats>().Sz;

        // Zresetowanie koloru wszystkich pol
        grid.ResetTileColors();
    }

    public void SetCharge()
    {
        if (Charge || Run)
            ResetChargeAndRun();

        // Ustalenie ktora postac jest wybrana
        GameObject character = Character.selectedCharacter;
        if (character == null) return;

        if (character.GetComponent<Stats>().actionsLeft < 2 && GameManager.StandardMode)
        {
            messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
            Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
            return;
        }

        else if (!Charge)
        {
            Charge = true;
            character.GetComponent<Stats>().tempSz = character.GetComponent<Stats>().Sz * 2; // zmiana aktualnej predkosci
            Run = false;
            GameObject.Find("AttackManager").GetComponent<AttackManager>().SelectTarget(false);
        }

        // Zresetowanie koloru wszystkich pol
        grid.ResetTileColors();

        // Aktualizacja podswietlonego zasiegu ruchu postaci
        HighlightTilesInMovementRange(character);
    }

    public void SetRun()
    {
        if (Charge || Run)
            ResetChargeAndRun();

        // Ustalenie ktora postac jest wybrana
        GameObject character = Character.selectedCharacter;
        if (character == null) return;

        if (character.GetComponent<Stats>().actionsLeft < 2 && GameManager.StandardMode)
        {
            messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
            Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
            return;
        }

        if (!Run)
        {
            Run = true;
            character.GetComponent<Stats>().tempSz = character.GetComponent<Stats>().Sz * 3; // zmiana aktualnej predkosci
            Charge = false;
            ActiveMove();
        }
    }

    //aktywuje mozliwosc ruchu
    public void ActiveMove()
    {
        canMove = true;

        //wylaczanie widocznosci buttonow akcji
        if (Character.trSelect != null && GameObject.Find("ActionsButtons/Canvas") != null)
        {
            GameObject.Find("ActionsButtons/Canvas").SetActive(false);

            HighlightTilesInMovementRange(Character.selectedCharacter);
        }
    }

    #region Move function
    public void MoveSelectedCharacter(GameObject selectedTile, GameObject character)
    {
        // Sprawdza, czy akcja wykonywania ruchu jest aktywna
        if (canMove)
        {
            // Sprawdza zasieg ruchu postaci
            int movementRange = character.GetComponent<Stats>().tempSz;

            // Pozycja postaci przed zaczeciem wykonywania ruchu
            Vector3 startCharPos = character.transform.position;

            // Pozycja pola wybranego jako cel ruchu
            Vector3 selectedTilePos = new Vector3(selectedTile.transform.position.x, selectedTile.transform.position.y, 0);

            // Znajdź najkrótszą ścieżkę do celu
            List<Vector3> path = FindPath(startCharPos, selectedTilePos, movementRange);

            // Sprawdza czy wybrane pole jest w zasiegu ruchu postaci. Warunek ten nie jest konieczny w przypadku automatycznej walki, dlatego dochodzi drugi warunek.
            if (path.Count > 0 && (path.Count <= movementRange|| GameManager.AutoMode))
            {

                if (Run || Charge)
                {
                    if(character.GetComponent<Stats>().actionsLeft == 2)
                        characterManager.TakeDoubleAction(character.GetComponent<Stats>());
                    else if (GameManager.StandardMode)
                    {
                        messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
                        Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
                        return;
                    }
                }
                else if (character.GetComponent<Stats>().actionsLeft > 0)
                    characterManager.TakeAction(character.GetComponent<Stats>());
                else if (GameManager.StandardMode)
                {
                    messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
                    Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
                    return;
                }

                // Odznacza postać na czas wykonywanania ruchu
                if (!Charge)
                {
                    character.GetComponent<Character>().SelectOrDeselectCharacter(character);
                    GameObject.Find("ActionsButtons").transform.Find("Canvas").gameObject.SetActive(false);
                }

                // Oznacza wybrane pole jako zajęte (gdyz troche potrwa, zanim postać tam dojdzie i gdyby nie zaznaczyć, to można na nie ruszyć inna postacią)
                selectedTile.GetComponent<Tile>().isOccupied = true;

                // Sprawdza, czy ruch spowoduje atak okazyjny
                CheckForOpportunityAttack(character, selectedTilePos);

                StartCoroutine(MoveCharacterWithDelay());

                // Wykonuje pojedynczy ruch tyle razy ile wynosi zasieg ruchu postaci
                IEnumerator MoveCharacterWithDelay()
                {
                    for (int i = 0; i < movementRange; i++)
                    {
                        if (character.transform.position == selectedTilePos)
                            break;

                        Vector3 nextPos = path[i];
                        nextPos.z = 0f;

                        float elapsedTime = 0f;
                        float duration = 0.2f; // Czas trwania interpolacji

                        // W AUTOMODE MUSZE TO NA RAZIE WYŁĄCZYĆ, BO PRZECIWNICY SĄ ZABIJANI I USUWANI W TRAKCIE ANIMACJI I PRZEZ TO WYWALA NULL REFERENCE
                        if (GameManager.AutoMode)
                            duration = 0f;

                        while (elapsedTime < duration)
                        {
                            isMoving = true;                        
                            
                            character.transform.position = Vector3.Lerp(character.transform.position, nextPos, elapsedTime / duration);
                            elapsedTime += Time.deltaTime;
                            yield return null; // Poczekaj na odświeżenie klatki animacji
                        }

                        character.transform.position = nextPos;
                    }

                    if (character.transform.position == selectedTilePos)
                    {
                        ResetChargeAndRun();
                        canMove = false;
                        isMoving = false;

                        // Ponownie zaznacza postać                           
                        character.GetComponent<Character>().SelectOrDeselectCharacter(character);
                        Character.trSelect = character.transform;
                    }
                }
            }
            else
            {
                GameObject.Find("ActionsButtons").transform.Find("Canvas").gameObject.SetActive(true);

                messageManager.ShowMessage("<color=red>Wybrane pole jest poza zasięgiem ruchu postaci.</color>", 4f);
                Debug.Log("Wybrane pole jest poza zasięgiem ruchu postaci.");
            }

            // Zresetowanie koloru podswietlonych pol w zasiegu ruchu
            grid.ResetTileColors();

            canMove = false;
            ResetChargeAndRun();
        }
    }


    // Funkcja obliczająca odległość pomiędzy dwoma punktami na płaszczyźnie XY
    private int CalculateDistance(Vector3 a, Vector3 b)
    {
        return (int)(Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y));
    }

    public List<Vector3> FindPath(Vector3 start, Vector3 goal, int movementRange)
    {
        // Tworzy listę otwartych węzłów
        List<Node> openNodes = new List<Node>();

        // Dodaje węzeł początkowy do listy otwartych węzłów
        Node startNode = new Node
        {
            Position = start,
            G = 0,
            H = CalculateDistance(start, goal),
            F = 0 + CalculateDistance(start, goal),
            Parent = default
        };
        openNodes.Add(startNode);

        // Tworzy listę zamkniętych węzłów
        List<Vector3> closedNodes = new List<Vector3>();

        while (openNodes.Count > 0)
        {
            // Znajduje węzeł z najmniejszym kosztem F i usuwa go z listy otwartych węzłów
            Node current = openNodes.OrderBy(n => n.F).First();
            openNodes.Remove(current);

            // Dodaje bieżący węzeł do listy zamkniętych węzłów
            closedNodes.Add(current.Position);

            // Sprawdza, czy bieżący węzeł jest węzłem docelowym
            if (current.Position == goal)
            {
                // Tworzy listę punktów i dodaje do niej węzły od węzła docelowego do początkowego
                List<Vector3> path = new List<Vector3>();
                Node node = current;

                while (node.Position != start)
                {
                    path.Add(new Vector3(node.Position.x, node.Position.y, 0));
                    node = node.Parent;
                }

                // Odwraca kolejność punktów w liście, aby uzyskać ścieżkę od początkowego do docelowego
                path.Reverse();

                return path;
            }

            // Pobiera sąsiadów bieżącego węzła
            List<Node> neighbors = new List<Node>();
            neighbors.Add(new Node { Position = current.Position + Vector3.up });
            neighbors.Add(new Node { Position = current.Position + Vector3.down });
            neighbors.Add(new Node { Position = current.Position + Vector3.left });
            neighbors.Add(new Node { Position = current.Position + Vector3.right });

            // Przetwarza każdego sąsiada
            foreach (Node neighbor in neighbors)
            {
                // Sprawdza, czy sąsiad jest w liście zamkniętych węzłów lub poza zasięgiem ruchu postaci
                if (closedNodes.Contains(neighbor.Position) || CalculateDistance(current.Position, neighbor.Position) > movementRange)
                {
                    continue; // przerywa tą iterację i przechodzi do kolejnej bez wykonywania w obecnej iteracji kodu, który jest poniżej. Natomiast 'break' przerywa całą pętle i kolejne iteracje nie wystąpią
                }

                // Sprawdza, czy na miejscu sąsiada występuje inny collider niż tile
                Collider2D collider = Physics2D.OverlapCircle(neighbor.Position, 0.1f);

                if (collider != null)
                {
                    bool isTile = false;

                    if (collider.gameObject.CompareTag("Tile") && !collider.gameObject.GetComponent<Tile>().isOccupied)
                    {
                        isTile = true;
                    }


                    if (isTile)
                    {
                        // Oblicza koszt G dla sąsiada
                        int gCost = current.G + 1;

                        // Sprawdza, czy sąsiad jest już na liście otwartych węzłów
                        Node existingNode = openNodes.Find(n => n.Position == neighbor.Position);

                        if (existingNode != null)
                        {
                            // Jeśli koszt G dla bieżącego węzła jest mniejszy niż dla istniejącego węzła, to aktualizuje go
                            if (gCost < existingNode.G)
                            {
                                existingNode.G = gCost;
                                existingNode.F = existingNode.G + existingNode.H;
                                existingNode.Parent = current;
                            }
                        }
                        else
                        {
                            // Jeśli sąsiad nie jest jeszcze na liście otwartych węzłów, to dodaje go
                            Node newNode = new Node
                            {
                                Position = neighbor.Position,
                                G = gCost,
                                H = CalculateDistance(neighbor.Position, goal),
                                F = gCost + CalculateDistance(neighbor.Position, goal),
                                Parent = current
                            };
                            openNodes.Add(newNode);
                        }
                    }
                }
            }
        }

        // Jeśli nie udało się znaleźć ścieżki, to zwraca pustą listę
        return new List<Vector3>();
    }
    #endregion

    #region Check for opportunity attack
    // Sprawdza czy ruch powoduje atak okazyjny
    public void CheckForOpportunityAttack(GameObject movingCharacter, Vector3 selectedTilePosition)
    {
        if (movingCharacter.CompareTag("Player"))
        {
            // Stworzenie listy wszystkich wrogow
            GameObject[] nearEnemies;
            nearEnemies = GameObject.FindGameObjectsWithTag("Enemy");

            // Atak okazyjny wywolywany dla kazdego wroga bedacego w zwarciu z bohaterem gracza
            foreach (GameObject enemy in nearEnemies)
            {
                // Sprawdzenie ilu wrogow jest w zwarciu z bohaterem gracza i czy ruch bohatera gracza powoduje oddalenie sie od nich (czyli atak okazyjny)
                float distanceFromOpponent = Vector3.Distance(movingCharacter.transform.position, enemy.transform.position);
                float distanceFromOpponentAfterMove = Vector3.Distance(selectedTilePosition, enemy.transform.position);

                if (distanceFromOpponent <= 1.8f && distanceFromOpponentAfterMove > 1.8f)
                {
                    // Wywolanie ataku okazyjnego w klasie AttackManager
                    AttackManager attackManager = GameObject.Find("AttackManager").GetComponent<AttackManager>();
                    attackManager.OpportunityAttack(enemy, movingCharacter);
                }
            }
        }
        if (movingCharacter.CompareTag("Enemy"))
        {
            // Stworzenie listy wszystkich bohaterow graczy
            GameObject[] nearPlayers;
            nearPlayers = GameObject.FindGameObjectsWithTag("Player");

            // Atak okazyjny wywolywany dla kazdego bohatera gracza bedacego w zwarciu z wrogiem
            foreach (GameObject player in nearPlayers)
            {
                // Sprawdzenie ilu bohaterow gracza jest w zwarciu z wrogiem i czy ruch wroga powoduje oddalenie sie od nich (czyli atak okazyjny)
                float distanceFromOpponent = Vector3.Distance(movingCharacter.transform.position, player.transform.position);
                float distanceFromOpponentAfterMove = Vector3.Distance(selectedTilePosition, player.transform.position);

                if (distanceFromOpponent <= 1.8f && distanceFromOpponentAfterMove > 1.8f)
                {
                    // Wywolanie ataku okazyjnego w klasie AttackManager
                    AttackManager attackManager = GameObject.Find("AttackManager").GetComponent<AttackManager>();
                    attackManager.OpportunityAttack(player, movingCharacter);
                }
            }
        }
    }
    #endregion

    #region Highlight movement range
    // Zmienia kolor wszystkich pol w zasiegu ruchu postaci
    public void HighlightTilesInMovementRange(GameObject character)
    {
        tilesInMovementRange.Clear();

        // Sprawdza zasieg ruchu postaci
        int movementRange = character.GetComponent<Stats>().tempSz;

        if (movementRange == 0)
            return;

        // Wrzucajac do listy postac, dodajemy punkt poczatkowy, ktory jest potrzebny do pozniejszej petli wyszukujacej dostepne pozycje
        tilesInMovementRange.Add(character);

        // wektor w prawo, lewo, góra, dół
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down };

        // Wykonuje pojedynczy ruch tyle razy ile wynosi zasieg ruchu postaci
        for (int i = 0; i < movementRange; i++)
        {
            // Lista pol, ktore bedziemy dodawac do listy wszystkich pol w zasiegu ruchu
            List<GameObject> tilesToAdd = new List<GameObject>();

            foreach (var tile in tilesInMovementRange)
            {
                // Szuka pol w każdym kierunku
                foreach (Vector3 direction in directions)
                {
                    // znajdź kolider z tagiem "Tile" przylegajacy do postaci na ktorym nie stoi inna postac (gdy stoi to collider wykryje najpierw obiekt postaci i nie wykryje 'tile')
                    Collider2D collider = Physics2D.OverlapCircle(tile.transform.position + direction, 0.1f);

                    // Jezeli collider to 'Tile' to dodajemy go do listy
                    if (collider != null && collider.gameObject.tag == "Tile")
                    {
                        tilesToAdd.Add(collider.gameObject);
                    }
                }
            }
            // Dodajemy do listy wszystkie pola, ktorych tam jeszcze nie ma
            foreach (var tile in tilesToAdd)
            {
                if(!tilesInMovementRange.Contains(tile))
                    tilesInMovementRange.Add(tile);
            }

            // Usuwamy postac z listy, bo nie jest ona 'Tile' :)
            tilesInMovementRange.Remove(character);
        }

        foreach (var tile in tilesInMovementRange)
        {
            // Zmienia kolor pola na highlightColor. Jesli pole juz jest podswietlone to przywraca domyslny kolor
            if (tile.GetComponent<Tile>()._renderer.material.color != tile.GetComponent<Tile>().rangeColor)
                tile.GetComponent<Tile>()._renderer.material.color = tile.GetComponent<Tile>().rangeColor;
        }
    }
    #endregion
}


public class Node
{
    public Vector3 Position; // Pozycja węzła na siatce
    public int G; // Koszt dotarcia do węzła
    public int H; // Szacowany koszt dotarcia z węzła do celu
    public int F; // Całkowity koszt (G + H)
    public Node Parent; // Węzeł nadrzędny w ścieżce
}

