using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class Character : MonoBehaviour
{
    public static Stats charStats;
    public enum Rasa { Człowiek, Elf, Krasnolud, Niziołek, Goblin, Minotaur, Ogr, Ork, Skaven, Smok, Troll, Zwierzoczłek }
    public Rasa rasa;

    private TMP_Text nameDisplay;
    private TMP_Text healthDisplay;
    private TMP_Text initiativeDisplay;

    public static GameObject selectedCharacter;
    public static Transform trSelect = null;

    private ButtonManager buttonManager;

    [HideInInspector] public AttackManager attackManager;

    private CharacterManager characterManager;

    private MessageManager messageManager;

    private MovementManager movementManager;

    void Start()
    {
        // Odniesienie do menadżera postaci
        characterManager = GameObject.Find("CharacterManager").GetComponent<CharacterManager>(); 

        // Odniesienie do Menadzera Wiadomosci wyswietlanych na ekranie gry
        messageManager = GameObject.Find("MessageManager").GetComponent<MessageManager>();

        // Odniesienie do Managera Przyciskow
        buttonManager = GameObject.Find("ButtonManager").GetComponent<ButtonManager>();

        //Odniesienie do Managera Ataku
        attackManager = GameObject.Find("AttackManager").GetComponent<AttackManager>();

        //Odniesienie do Managera Ruchu
        movementManager = GameObject.Find("MovementManager").GetComponent<MovementManager>();

        //nadanie rasy
        if (this.gameObject.CompareTag("Player"))
            rasa = (Rasa)UnityEngine.Random.Range(0, 4);
        else
            rasa = (Rasa)UnityEngine.Random.Range(4, Enum.GetValues(typeof(Rasa)).Length); 

        //messageManager.ShowMessage($"Stworzyłeś {this.gameObject.name} o rasie {rasa}", 3f);
        //Debug.Log($"Stworzyłeś {this.gameObject.name} o rasie {rasa}");

        //nadanie temu obiektowi klasy Stats
        charStats = this.gameObject.AddComponent<Stats>();
        this.gameObject.GetComponent<MovementManager>();

        // nadanie poczatkowego imienia takiego jak nazwa obiektu gry, np. Player 1
        charStats.Name = this.gameObject.name;

        //wygenerowanie poczatkowych statystyk w zaleznosci od rasy. Metoda ta jest zawarta w klasie Stats
        charStats.SetBaseStatsByRace(rasa);

        if(this.gameObject.CompareTag("Player"))
        {
            healthDisplay = this.transform.Find("healthPointsPlayer").gameObject.GetComponent<TMP_Text>();
            initiativeDisplay = this.transform.Find("initiativePlayer").gameObject.GetComponent<TMP_Text>();
            nameDisplay = this.transform.Find("namePlayer").gameObject.GetComponent<TMP_Text>();
        }
        else if (this.gameObject.CompareTag("Enemy"))
        {
            healthDisplay = this.transform.Find("healthPointsEnemy").gameObject.GetComponent<TMP_Text>();
            initiativeDisplay = this.transform.Find("initiativeEnemy").gameObject.GetComponent<TMP_Text>();
            nameDisplay = this.transform.Find("nameEnemy").gameObject.GetComponent<TMP_Text>();
        }

        healthDisplay.transform.position = this.gameObject.transform.position;

        initiativeDisplay.transform.position = new Vector3(this.gameObject.transform.position.x + 0.5f, this.gameObject.transform.position.y + 0.5f, this.gameObject.transform.position.z);

        nameDisplay.transform.position = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y - 0.5f, this.gameObject.transform.position.z);
    }

    void Update()
    {
        // wyswietlanie na biezaco aktualnych punktow zycia, imienia oraz inicjatywy
        healthDisplay.text = this.gameObject.GetComponent<Stats>().tempHealth + "/" + this.gameObject.GetComponent<Stats>().maxHealth;
        initiativeDisplay.text = this.gameObject.GetComponent<Stats>().Initiative.ToString();
        nameDisplay.text = this.gameObject.GetComponent<Stats>().Name.ToString();
 
        if (charStats.tempHealth < 0 && charStats.criticalCondition == false)
        {
            characterManager.GetCriticalHit(charStats);
        }
    }

    private void OnMouseEnter()
    {
        if (MagicManager.targetSelecting && selectedCharacter.GetComponent<Stats>().AreaSize > 0)
        {
            GameObject.Find("MagicManager").GetComponent<MagicManager>().HighlightTilesInSpellRange(this.gameObject);
        }

        // Podświetla potencjalną ścieżke postaci podczas wyboru celu szarży
        if (MovementManager.Charge && selectedCharacter != this.gameObject && selectedCharacter.tag != this.gameObject.tag)
        {
            // Sprawdza dystans do pola docelowego
            Vector3 targetTilePos = attackManager.GetTargetTilePosition(selectedCharacter, this.gameObject);
            List<Vector3> path = movementManager.FindPath(selectedCharacter.transform.position, targetTilePos, selectedCharacter.GetComponent<Stats>().tempSz);

            if (path.Count <= selectedCharacter.GetComponent<Stats>().tempSz)
            {
                foreach (Vector3 tile in path)
                {
                    Collider2D collider = Physics2D.OverlapCircle(tile, 0.1f);
                    collider.gameObject.GetComponent<Tile>()._renderer.material.color = collider.gameObject.GetComponent<Tile>().rangeHighlightColor;
                }
            }
        }
    }
    void OnMouseExit()
    {
        // Resetuje podświetlenie potencjalnej ścieżki postaci podczas wyboru celu szarży
        if (MovementManager.Charge)
        {
            GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
            foreach (var tile in tiles)
            {
                if (tile.GetComponent<Tile>()._renderer.material.color == tile.GetComponent<Tile>().rangeHighlightColor)
                    tile.GetComponent<Tile>()._renderer.material.color = tile.GetComponent<Tile>().rangeColor;
            }
        }
    }

    #region Select or deselect character method
    public void OnMouseDown()
    {
        if (MovementManager.isMoving || GameManager.PanelIsOpen)
            return;

        if (this.gameObject.GetComponent<Stats>().actionsLeft == 0 && GameManager.StandardMode && trSelect == null)
        {
            messageManager.ShowMessage($"<color=red>Wybrana postać nie może wykonać więcej akcji w tej rundzie.</color>", 4f);
            Debug.Log("Wybrana postać nie może wykonać więcej akcji w tej rundzie.");

            return;
        }

        SelectOrDeselectCharacter(this.gameObject);
    }

    public void SelectOrDeselectCharacter(GameObject thisCharacter)
    {
        GridManager grid = GameObject.Find("Grid").GetComponent<GridManager>();

        // Umozliwia zaznaczenie/odznaczenie postaci, tylko gdy inne postacie nie sa wybrane i panel edycji statystyk jest zamkniety
        if (!AttackManager.targetSelecting && (trSelect == null || trSelect == transform))
        {
            if (trSelect == thisCharacter.transform) // klikniecie na postac, ktora juz jest wybrana
            {
                thisCharacter.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                trSelect = null;
                MovementManager.canMove = false;
                // Resetuje tryb wyboru celu ataku, gdyby był aktywny
                AttackManager.targetSelecting = false;
                MagicManager.targetSelecting = false;

                if (selectedCharacter.CompareTag("Player"))
                    selectedCharacter.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
                else if (selectedCharacter.CompareTag("Enemy"))
                    selectedCharacter.GetComponent<Renderer>().material.color = new Color(255, 0, 0);

                buttonManager.ShowOrHideActionsButtons(selectedCharacter, false);

                // Zresetowanie koloru podswietlonych pol w zasiegu ruchu
                grid.ResetTileColors();
            }
            else // klikniecie na postac, gdy zadna postac nie jest wybrana
            {
                trSelect = thisCharacter.transform;
                thisCharacter.transform.localScale = new Vector3(1f, 1f, 1f);

                selectedCharacter = thisCharacter;

                if (selectedCharacter.CompareTag("Player"))
                    selectedCharacter.GetComponent<Renderer>().material.color = new Color(0f, 1f, 0.64f);
                else if (selectedCharacter.CompareTag("Enemy"))
                    selectedCharacter.GetComponent<Renderer>().material.color = new Color(1.0f, 0.64f, 0.0f);

                buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);

                //Zresetowanie szarzy i biegu
                GameObject.Find("MovementManager").GetComponent<MovementManager>().ResetChargeAndRun();

                // Zresetowanie koloru podswietlonych pol w zasiegu ruchu
                grid.ResetTileColors();
            }

            // Ukrywa przyciski zaklęć, jeśli są aktywne
            buttonManager.HideSpellButtons();
        }

        // Jezeli jest aktywny tryb wybierania celu ataku, przekazuje informacje o kliknietej postaci i wywoluje funkcje Attack traktujac wybraną postać jako atakujacego, a klikniętą jako atakowanego.
        if (AttackManager.targetSelecting == true)
        {
            // Jezeli jest rzucane zaklęcie i nie jest to zaklęcie ofensywne to wywoływana jest funkcja zaklęcia leczącego
            if (MagicManager.targetSelecting == true && selectedCharacter.GetComponent<Stats>().OffensiveSpell == false)
            {
                GameObject.Find("MagicManager").GetComponent<MagicManager>().HealingSpell(thisCharacter);

                // Przywraca widocznosc przyciskow akcji atakujacej postaci
                buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);
                // Ukrywa przyciski zaklęć, jeśli są aktywne
                buttonManager.HideSpellButtons();

                // Resetuje tryb wyboru celu ataku
                AttackManager.targetSelecting = false;
                MagicManager.targetSelecting = false;

                GameObject.Find("MagicManager").GetComponent<MagicManager>().ResetHighlightTilesInSpellRange();

                return;
            }

            // Sprawdza, czy atakujacym nie jest sojusznik
            if (selectedCharacter.tag == thisCharacter.tag && !MagicManager.targetSelecting)
            {
                messageManager.ShowMessage($"<color=red>Nie możesz atakować swoich sojuszników.</color>", 3f);
                Debug.Log("Nie możesz atakować swoich sojuszników.");

                // Przywraca widocznosc przyciskow akcji
                buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);

                // Ukrywa przyciski zaklęć, jeśli są aktywne
                buttonManager.HideSpellButtons();

                // Resetuje szarze jesli jest aktywna
                if (MovementManager.Charge)
                    GameObject.Find("MovementManager").GetComponent<MovementManager>().ResetChargeAndRun();

                // Resetuje tryb wyboru celu ataku
                AttackManager.targetSelecting = false;
                MagicManager.targetSelecting = false;

                return;
            }

            if (!MovementManager.Charge && MagicManager.targetSelecting != true)
            {
                attackManager.Attack(selectedCharacter, thisCharacter);
                // Przywraca widocznosc przyciskow akcji
                buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);
            }
            else if (MovementManager.Charge)
                attackManager.ChargeAttack(selectedCharacter, thisCharacter);
            else if (MagicManager.targetSelecting == true)
            {
                GameObject.Find("MagicManager").GetComponent<MagicManager>().GetMagicDamage(thisCharacter);
                // Przywraca widocznosc przyciskow akcji atakujacej postaci
                buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);
            }

            // Ukrywa przyciski zaklęć, jeśli są aktywne
            buttonManager.HideSpellButtons();

            // Resetuje tryb wyboru celu ataku
            AttackManager.targetSelecting = false;
            MagicManager.targetSelecting = false;
        }
    }
    #endregion

    #region Select with right or middle mouse button
    void OnMouseOver()
    {
        GridManager grid = GameObject.Find("Grid").GetComponent<GridManager>();

        // PRAWY PRZYCISK MYSZY == Zaatakuj klikniętą postać
        if (Input.GetMouseButtonDown(1) && !GameManager.PanelIsOpen && trSelect != null && !MovementManager.isMoving && MagicManager.targetSelecting != true) // wciśnięcie prawego przycisku myszy
        {
            // Sprawdza, czy atakujacym nie jest sojusznik
            if (selectedCharacter.tag == this.gameObject.tag)
            {
                messageManager.ShowMessage($"<color=red>Nie możesz atakować swoich sojuszników.</color>", 3f);
                Debug.Log("Nie możesz atakować swoich sojuszników.");

                if (!MovementManager.Charge)
                {
                    // Przywraca widocznosc przyciskow akcji
                    buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);
                }
                // Resetuje szarze jesli jest aktywna
                if (MovementManager.Charge)
                    GameObject.Find("MovementManager").GetComponent<MovementManager>().ResetChargeAndRun();
                return;
            }

            if (!MovementManager.Charge)
            {
                attackManager.Attack(selectedCharacter, this.gameObject);
                // Przywraca widocznosc przyciskow akcji
                buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);
            }
            else
                attackManager.ChargeAttack(selectedCharacter, this.gameObject);
        }
        // ŚRODKOWY PRZYCISK MYSZY == Wybierz klikniętą postać, nie musisz odznaczać obecnie wybranej
        else if (Input.GetMouseButtonDown(2) && !GameManager.PanelIsOpen && !MovementManager.isMoving) // wcisniecie srodkowego przycisku myszy
        {
            if (this.gameObject.GetComponent<Stats>().actionsLeft == 0 && GameManager.StandardMode)
            {
                messageManager.ShowMessage($"<color=red>Wybrana postać nie może wykonać więcej akcji w tej rundzie.</color>", 4f);
                Debug.Log("Wybrana postać nie może wykonać więcej akcji w tej rundzie.");

                return;
            }

            // Zresetowanie zmiany koloru i wielkosci poprzednio wybranej posataci
            if (selectedCharacter != null)
            {
                selectedCharacter.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                
                buttonManager.ShowOrHideActionsButtons(selectedCharacter, false);

                if (selectedCharacter.CompareTag("Player"))
                    selectedCharacter.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
                else if (selectedCharacter.CompareTag("Enemy"))
                    selectedCharacter.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
            }

            trSelect = transform;
            transform.localScale = new Vector3(1f, 1f, 1f);

            selectedCharacter = this.gameObject;

            // Aktualizacja wyświetlanych statystyk
            GameObject.Find("StatsEditor").GetComponent<StatsEditor>().LoadAttributes();

            if (selectedCharacter.CompareTag("Player"))
                selectedCharacter.GetComponent<Renderer>().material.color = new Color(0f, 1f, 0.64f);
            else if (selectedCharacter.CompareTag("Enemy"))
                selectedCharacter.GetComponent<Renderer>().material.color = new Color(1.0f, 0.64f, 0.0f);

            // Ukrywa przyciski zaklęć, jeśli są aktywne
            buttonManager.HideSpellButtons();

            buttonManager.ShowOrHideActionsButtons(selectedCharacter, true);

            MovementManager.canMove = false;
            // Resetuje tryb wyboru celu ataku, gdyby był aktywny
            AttackManager.targetSelecting = false;
            MagicManager.targetSelecting = false;

            //Zresetowanie szarzy i biegu
            GameObject.Find("MovementManager").GetComponent<MovementManager>().ResetChargeAndRun();

            // Zresetowanie koloru podswietlonych pol w zasiegu ruchu
            grid.ResetTileColors();       
        }
    }
    #endregion


}





