using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class CharacterManager : MonoBehaviour
{
    [Header("Referencje do prefabu Playera i Enemy")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject enemyObject;
    GameObject newCharacter;

    public static bool characterAdding; // bool aktywny podczas wybierania pola na którym chcemy stworzyć postać
    public static bool randomPositionMode; // bool określający, czy postać ma się tworzyć w losowej pozycji
    public static string characterTag; // określa, czy mamy do czynienia z Playerem, czy z Enemy


    public static int playersAmount; // liczba wszystkich bohaterow graczy
    public static int enemiesAmount; // liczba wszystkich wrogów graczy

    [Header("Przyciski dotyczące zaznaczonej postaci")]
    [SerializeField] private GameObject setStatsButton;
    [SerializeField] private GameObject destroyButton;
    [SerializeField] private GameObject rollButton;
    [SerializeField] private GameObject endTurnButton;
    [SerializeField] private GameObject statsDisplayPanel;

    private bool attributesLoaded;
    public static bool Autosave;

    void Start()
    {
        playersAmount = 0;
        enemiesAmount = 0;
        randomPositionMode = false;
        characterAdding = false;
    }

    void Update()
    {
        // Wylacza widocznosc przycisku zmiany statystyk i usuwania postaci, jesli postac nie jest wybrana
        if (Character.trSelect == null)
        {
            destroyButton.SetActive(false);
            setStatsButton.SetActive(false);
            rollButton.SetActive(false);
            endTurnButton.SetActive(false);
            statsDisplayPanel.SetActive(false);
            attributesLoaded = false;
        }
        else if (Character.selectedCharacter != null)
        {
            destroyButton.SetActive(true);
            setStatsButton.SetActive(true);
            rollButton.SetActive(true);
            endTurnButton.SetActive(true);
            statsDisplayPanel.SetActive(true);
            GameObject.Find("ButtonManager").GetComponent<ButtonManager>().RefreshWeaponTypeButtons(Character.selectedCharacter);
            if (!attributesLoaded)
            {
                GameObject.Find("StatsEditor").GetComponent<StatsEditor>().LoadAttributes();               
                attributesLoaded = true;
            }

            if (Character.selectedCharacter.GetComponent<Stats>().actionsLeft == 0 && GameManager.StandardMode && !MovementManager.isMoving)
            {
                Stats[] allStatsArray = FindObjectsOfType<Stats>();
                Array.Sort(allStatsArray, (x, y) => y.Initiative.CompareTo(x.Initiative));

                // Konwersja tablicy na listę
                List<Stats> allStatsList = new List<Stats>(allStatsArray);
                
                // Usunięcie elementów z listy
                for (int i = allStatsList.Count - 1; i >= 0; i--)
                {
                    if (allStatsList[i].actionsLeft == 0)
                        allStatsList.RemoveAt(i);
                }

                if (allStatsList.Count < 1)
                {
                    Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(Character.selectedCharacter);
                    GameObject.Find("RoundManager").GetComponent<RoundManager>().NextRound();
                    return;
                }

                GameObject nextCharacter = allStatsList[0].gameObject;

                Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(Character.selectedCharacter);
                Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(nextCharacter);

                // Zresetowanie pozycji obronnej, jeśli jest aktywna
                if (Character.selectedCharacter.GetComponent<Stats>().defensiveBonus > 0)
                    GameObject.Find("AttackManager").GetComponent<AttackManager>().DefensivePosition(GameObject.Find("DefensivePositionButton"));

                // Aktualizacja wyświetlanych statystyk
                Character.selectedCharacter = nextCharacter;
                GameObject.Find("StatsEditor").GetComponent<StatsEditor>().LoadAttributes();
            }
        }
    }

    #region Create new character functions

    public void SetRandomPositionMode(GameObject button)
    {
        if(randomPositionMode)
        {
            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
            randomPositionMode = false;

            GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage("Losowa pozycja postaci wyłączona.", 3f);
            Debug.Log("Losowa pozycja postaci wyłączona.");
        }
        else
        {
            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            randomPositionMode = true;
            GameObject.Find("CreatePlayerButton").GetComponent<Image>().color = new Color(1 / 2.55f, 1f, 1 / 2.55f, 1f);
            GameObject.Find("CreateEnemyButton").GetComponent<Image>().color = new Color(1f, 1 / 2.55f, 1 / 2.55f, 1f);
            characterAdding = false;

            GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage("Losowa pozycja postaci włączona.", 3f);
            Debug.Log("Losowa pozycja postaci włączona.");
        }
    }
    public void SelectFieldForNewCharacter(string tag)
    {
        if (MovementManager.isMoving)
            return;

        characterAdding = true;
        characterTag = tag;

        if(randomPositionMode)
        {
            CreateNewCharacter(characterTag, "", Vector2.zero);
            return;
        }

        // Zmniejszenie przezroczystosci przycisku tworzenia postaci
        if (tag == "Player")
        {
            GameObject.Find("CreatePlayerButton").GetComponent<Image>().color = new Color(1 / 2.55f, 1f, 1 / 2.55f, 0.5f);
            GameObject.Find("CreateEnemyButton").GetComponent<Image>().color = new Color(1f, 1 / 2.55f, 1 / 2.55f, 1f);
        }
        else if (tag == "Enemy")
        {
            GameObject.Find("CreateEnemyButton").GetComponent<Image>().color = new Color(1f, 1 / 2.55f, 1 / 2.55f, 0.5f);
            GameObject.Find("CreatePlayerButton").GetComponent<Image>().color = new Color(1 / 2.55f, 1f, 1 / 2.55f, 1f);
        }

        GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage("Wybierz pole na którym chcesz postawić postać.", 3f);
        Debug.Log("Wybierz pole na którym chcesz postawić postać.");
    }

    public void CreateNewCharacter(string characterTag, string characterName, Vector2 position)
    {
        // Zresetowanie koloru przycisku tworzenia postaci
        if (characterTag == "Player")
            GameObject.Find("CreatePlayerButton").GetComponent<Image>().color = new Color(1 / 2.55f, 1f, 1 / 2.55f, 1f);
        else if (characterTag == "Enemy")
            GameObject.Find("CreateEnemyButton").GetComponent<Image>().color = new Color(1f, 1 / 2.55f, 1 / 2.55f, 1f);

        // Liczba dostępnych pól
        int availableTiles = GridManager.width * GridManager.height; // wymiary planszy

        bool xParzysty = (GridManager.width % 2 == 0) ? true : false; // zmienna potrzebna do prawidlowego generowania losowej pozycji postaci
        bool yParzysty = (GridManager.height % 2 == 0) ? true : false; // zmienna potrzebna do prawidlowego generowania losowej pozycji postaci

        // Sprawdzenie dostępności pól
        int attempts = 0;
        Collider2D searchForColliders;
        do
        {
            if(randomPositionMode)
            {
                int x;
                int y;

                // Generowanie losowej pozycji na mapie
                if (xParzysty)
                    x = UnityEngine.Random.Range(-GridManager.width / 2, GridManager.width / 2);
                else
                    x = UnityEngine.Random.Range(-GridManager.width / 2, GridManager.width / 2 + 1);
                if (yParzysty)
                    y = UnityEngine.Random.Range(-GridManager.height / 2, GridManager.height / 2);
                else
                    y = UnityEngine.Random.Range(-GridManager.height / 2, GridManager.height / 2 + 1);

                position = new Vector2(x, y);

                if (GridManager.width == 1)
                    position.x = 0;
                if (GridManager.height == 1)
                    position.y = 0;
            }


            // Sprawdzenie czy dane pole jest wolne czy zajęte
            searchForColliders = Physics2D.OverlapCircle(position, 0.1f);

            if (searchForColliders == null || searchForColliders.tag == "Tile" && availableTiles > 1)
            {
                // Zmniejszenie liczby dostępnych pól
                availableTiles--;
            }

            // Inkrementacja liczby prób
            attempts++;

            // Sprawdzenie, czy liczba prób nie przekracza maksymalnej liczby dostępnych pól
            if (attempts > availableTiles)
            {
                Debug.Log("Nie można utworzyć nowej postaci. Brak wolnych pól.");
                return;
            }
        }
        while (searchForColliders != null && searchForColliders.tag != "Tile");

        if (characterTag == "Player")
        {        
            //tworzy nową postać w losowej pozycji i nadaje mu odpowiednia nazwe
            newCharacter = Instantiate(playerObject, position, Quaternion.identity);

            playersAmount++;
            if( characterName.Length < 1)
                newCharacter.name = ("Player " + playersAmount);
            else
                newCharacter.name = characterName;

            newCharacter.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        }
        else if (characterTag == "Enemy")
        {
            //tworzy nową postać w losowej pozycji i nadaje mu odpowiednia nazwe
            newCharacter = Instantiate(enemyObject, position, Quaternion.identity);

            enemiesAmount++;
            if( characterName.Length < 1)
                newCharacter.name = ("Enemy " + enemiesAmount);
            else
                newCharacter.name = characterName;

            newCharacter.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        }

        newCharacter.GetComponent<Character>();
        characterAdding = false;

        // Wywoluje ustawienie poziomu postaci (opoznienie jest po to, aby inne operacje zdazyly sie wykonac)
        Invoke("SetCharacterLevel", 0.05f);
    }

    void SetCharacterLevel()
    {
        GameObject.Find("ExpManager").GetComponent<ExpManager>().SetCharacterLevel(newCharacter);
    }
    #endregion

    #region Select character with biggest initiative when new round starts
    public void SelectCharacterWithBiggestInitiative(bool scaryEnemyExist, Stats[] allObjectsWithStats)
    {
        // Rzut na strach dla wszystkich postaci graczy
        if (scaryEnemyExist)
            RollSW();

        Array.Sort(allObjectsWithStats, (x, y) => y.Initiative.CompareTo(x.Initiative));

        // Odznaczenie aktualnie zaznaczonej postaci
        foreach (var stats in allObjectsWithStats)
        {
            if (stats.gameObject.transform.localScale.x != 0.85f && Character.selectedCharacter != null)
            {
                Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(stats.gameObject);
            }
        }

        // Zaznaczenie postaci z najwyższą inicjatywą
        if (allObjectsWithStats.Length > 0 && allObjectsWithStats[0].gameObject.transform.localScale.x == 0.85f)
        {
            Character.selectedCharacter = allObjectsWithStats[0].gameObject;
            Character.selectedCharacter.GetComponent<Character>().SelectOrDeselectCharacter(Character.selectedCharacter);

            // Zresetowanie pozycji obronnej, jeśli jest aktywna
            if (Character.selectedCharacter.GetComponent<Stats>().defensiveBonus > 0)
                GameObject.Find("AttackManager").GetComponent<AttackManager>().DefensivePosition(GameObject.Find("DefensivePositionButton"));
        }

        // Aktualizacja wyświetlanych statystyk
        if(Character.selectedCharacter != null)
            GameObject.Find("StatsEditor").GetComponent<StatsEditor>().LoadAttributes();
    }
    #endregion

    #region Roll for players fear
    public void RollSW()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in allPlayers)
        {         
            if (!player.GetComponent<Stats>().isScared)
                continue;

            int rollResult = UnityEngine.Random.Range(1, 101);
            int bonus = 0;
            if (player.GetComponent<Stats>().Brave == true)
                bonus = 10;

            if (rollResult > (player.GetComponent<Stats>().SW + bonus))
            {
                player.GetComponent<Stats>().actionsLeft = 0;
                player.GetComponent<Stats>().isScared = true;

                GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage($"<color=red>{player.GetComponent<Stats>().Name} nie zdał testu strachu. Wynik rzutu: {rollResult}</color>", 5f);
                Debug.Log($"{player.GetComponent<Stats>().Name} nie zdał testu strachu. Wynik rzutu: {rollResult}");
            }
            else if (rollResult <= (player.GetComponent<Stats>().SW + bonus))
            {
                player.GetComponent<Stats>().isScared = false;
                GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage($"<color=green>{player.GetComponent<Stats>().Name} zdał test strachu. Wynik rzutu: {rollResult}</color>", 5f);
                Debug.Log($"{player.GetComponent<Stats>().Name} zdał test strachu. Wynik rzutu: {rollResult}");
            }
        }
    }
    #endregion

    #region Get critical hit function
    public void GetCriticalHit(Stats character)
    {
        character.criticalCondition = true;

        int criticalValue = UnityEngine.Random.Range(1, 101);

        GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage($"<color=red>Żywotność spadła poniżej 0.</color> Wynik rzutu na obrażenia krytyczne: <color=red>{criticalValue}</color>", 6f);
        Debug.Log("Żywotność spadła poniżej 0. Wynik rzutu na obrażenia krytyczne: " + criticalValue);    
    }
    #endregion

    #region Reset parry and dodge function
    public void ResetParryAndDodge(Stats charStats)
    {
        charStats.canParry = true;
        if (charStats.Dodge > 0) //sprawdzenie czy postac posiada zdolnosc Unik
            charStats.canDodge = true;
    }
    #endregion

    #region Reset spellcasting limit
    public void ResetSpellcastingLimit(Stats charStats)
    {
        charStats.canSpell = true;
    }
    #endregion

    #region Character actions managing
    public void ResetActionsNumber(Stats charStats)
    {
        charStats.actionsLeft = 2;
        charStats.attacksLeft = charStats.A;
    }

    public void TakeAction(Stats charStats) // wykonanie akcji
    {
        Autosave = true;
        GameObject.Find("SaveSystem").GetComponent<SaveSystem>().SaveAllCharactersStats();
        Autosave = false;

        if (!GameManager.StandardMode)
            return;

        charStats.actionsLeft--;
        Debug.Log($"{charStats.gameObject.name} wykonał akcję pojedynczą. Dostępne akcje w tej rundzie: {charStats.actionsLeft}");
    }

    public void TakeDoubleAction(Stats charStats) // wykonanie akcji podwójnej
    {
        Autosave = true;      
        GameObject.Find("SaveSystem").GetComponent<SaveSystem>().SaveAllCharactersStats();
        Autosave = false;

        if (!GameManager.StandardMode)
            return;

        charStats.actionsLeft = 0;
        Debug.Log($"{charStats.gameObject.name} wykonał akcję podwójną. Dostępne akcje w tej rundzie: {charStats.actionsLeft}");
    }

    public void UndoAction()
    {
        Autosave = true;
        GameObject.Find("SaveSystem").GetComponent<SaveSystem>().LoadAllCharactersStats(true);
        Autosave = false;
    }
    #endregion
}
