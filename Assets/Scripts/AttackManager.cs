using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class AttackManager : MonoBehaviour
{
    public static bool targetSelecting; // tryb wyboru celu ataku. Jego wartosc mowi o tym, czy w danym momencie trwa wybieranie celu ataku

    private float attackDistance; // dystans pomiedzy walczacymi
    private bool targetDefended; // informacja o tym, czy postaci udało się sparować lub uniknąć atak

    private int attackBonus; // sumaryczna premia do WW lub US przy ataku
    private int chargeBonus; //premia za szarżę
    private int defensiveBonus; // minus do atakow przeciwko postaci z pozycja obronna

    private CharacterManager characterManager;
    private MessageManager messageManager;
    private MovementManager movementManager;

    [SerializeField] private Animator attackAnimator;

    void Start()
    {
        // Odniesienie do Menadzera Wiadomosci wyswietlanych na ekranie gry
        messageManager = GameObject.Find("MessageManager").GetComponent<MessageManager>();

        // Odniesienie do Menadzera Ruchu
        movementManager = GameObject.Find("MovementManager").GetComponent<MovementManager>();

        // Odniesienie do menadżera postaci
        characterManager = GameObject.Find("CharacterManager").GetComponent<CharacterManager>();
    }

    #region Selecting target function
    // Wybor celu ataku
    public void SelectTarget(bool spellTarget)
    {
        messageManager.ShowMessage("Wybierz cel, klikając na niego.", 3f);
        Debug.Log("Wybierz cel, klikając na niego.");

        targetSelecting = true;
        
        if(spellTarget)
        {
            MagicManager.targetSelecting = true;

            // Ukrywa przyciski zaklęć, jeśli są aktywne
            GameObject.Find("ButtonManager").GetComponent<ButtonManager>().HideSpellButtons();
            return;
        }

        // Wylacza widocznosc przyciskow akcji postaci
        if(GameObject.Find("ActionsButtons/Canvas") != null)
            GameObject.Find("ActionsButtons/Canvas").SetActive(false);

    }
    #endregion

    #region Reload functions
    public void Reload()
    {
        GameObject character = Character.selectedCharacter;
        
        if (character.GetComponent<Stats>().reloadLeft > 0)
        {
            if (character.GetComponent<Stats>().actionsLeft > 0)
                characterManager.TakeAction(character.GetComponent<Stats>());
            else if (GameManager.StandardMode)
            {
                messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
                Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
                return;
            }

            character.GetComponent<Stats>().reloadLeft--;
        }
        if (character.GetComponent<Stats>().reloadLeft == 0)
        {
            messageManager.ShowMessage($"Broń <color=#00FF9A>{character.GetComponent<Stats>().Name}</color> załadowana.", 3f);
            Debug.Log($"Broń {character.GetComponent<Stats>().Name} załadowana.");
        }
        else
        {
            messageManager.ShowMessage($"Ładowanie broni <color=#00FF9A>{character.GetComponent<Stats>().Name}</color>. Pozostała/y {character.GetComponent<Stats>().reloadLeft} akcja/e.", 4f);
            Debug.Log($"Ładowanie broni {character.GetComponent<Stats>().Name}. Pozostała/y {character.GetComponent<Stats>().reloadLeft} akcja/e aby móc strzelić.");
        }      
    }
    #endregion

    #region Attack function
    public void Attack(GameObject attacker, GameObject target)
    {
        Stats attackerStats = attacker.GetComponent<Stats>();
        Stats targetStats = target.GetComponent<Stats>();

        do
        {
            //uwzględnienie bonusu do WW zwiazanego z szarżą
            if (MovementManager.Charge)
                chargeBonus = 10;
            else
                chargeBonus = 0;

            // Ustala bonus do trafienia (za szarżę i przycelowanie)
            attackBonus = attackerStats.aimingBonus + chargeBonus;

            // Sprawdza, czy atakowany posiada bonus za przyjecie pozycji obronnej
            defensiveBonus = targetStats.defensiveBonus;

            // liczy dystans pomiedzy walczacymi
            if (attacker != null && target != null)
                attackDistance = Vector3.Distance(attacker.transform.position, target.transform.position);

            // Jezeli jest wykonywana szarza, to zasieg ataku jest ustawiany na zasieg broni do walki wrecz
            if (MovementManager.Charge)
                attackDistance = 1.5f;

            // sprawdza, czy dystans miedzy walczacymi jest mniejszy lub rowny zasiegowi broni atakujacego (uwzględnia też długi zasięg w przypadku broni dystansowej)
            if (attackDistance <= attackerStats.AttackRange || attackDistance <= attackerStats.AttackRange * 2 && attackerStats.AttackRange > 1.5f || MovementManager.Charge)
            {
                if (attackDistance > 1.5f && !MovementManager.Charge && attackerStats.reloadLeft == 0)
                {
                    // Sprawdza, czy na linii strzału znajduje się przeszkoda
                    RaycastHit2D[] raycastHits = Physics2D.RaycastAll(attacker.transform.position, target.transform.position - attacker.transform.position, attackDistance);

                    foreach (var raycastHit in raycastHits)
                    {
                        if (raycastHit.collider != null && (raycastHit.collider.CompareTag("Tree") || raycastHit.collider.CompareTag("Wall")))
                        {
                            messageManager.ShowMessage($"<color=red>Na linii strzału znajduje się przeszkoda.</color>", 3f);
                            Debug.Log("Na linii strzału znajduje się przeszkoda.");
                            return;
                        }
                        if (raycastHit.collider != null && raycastHit.collider.CompareTag("Rock") && Vector3.Distance(raycastHit.collider.gameObject.transform.position, target.transform.position) <= 1.5f)
                        {
                            defensiveBonus += 20;
                        }
                    }
                }

                // WYKONANIE AKCJI               
                bool canTakeAction = attackerStats.A == 1 && attackerStats.actionsLeft > 0 && attackerStats.attacksLeft > 0 || attackerStats.A > 1 && attackerStats.actionsLeft == 1;
                bool canTakeDoubleAction = attackerStats.attacksLeft >= 1 && attackerStats.actionsLeft == 2 || attackerStats.actionsLeft == -1;

                if (!MovementManager.Charge && attackDistance <= 1.5f || attackDistance > 1.5f && attackerStats.reloadLeft == 0)
                {
                    if (canTakeAction)
                    {
                        attackerStats.attacksLeft--;
                        characterManager.TakeAction(attackerStats);
                    }
                    else if (canTakeDoubleAction)
                    {
                        attackerStats.actionsLeft = -1;

                        attackerStats.attacksLeft--;
                        if (attackerStats.attacksLeft == 0)
                            characterManager.TakeDoubleAction(attackerStats);
                    }
                    else if (GameManager.StandardMode)
                    {
                        messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
                        Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
                        return;
                    }
                }
                else if (!MovementManager.Charge && attackDistance > 1.5f && attackerStats.reloadLeft != 0)
                {
                    messageManager.ShowMessage($"<color=red>Broń wymaga naładowania</color>", 2f);
                    Debug.Log($"Broń wymaga naładowania");
                    return;
                }


                int wynik = UnityEngine.Random.Range(1, 101);
                bool hit = false;

                // sprawdza czy atak jest atakiem dystansowym
                if (attackDistance > 1.5f && !MovementManager.Charge)
                {
                    attackerStats.distanceFight = true;
                
                    // Sprawdza, czy strzał jest wykonywany na długi zasięg broni i jeśli tak to dodaje modyfikator -20
                    attackBonus -= attackDistance > attackerStats.AttackRange ? 20 : 0;

                    hit = wynik <= attackerStats.US + attackBonus - defensiveBonus; // zwraca do 'hit' wartosc 'true' jesli to co jest po '=' jest prawda. Jest to skrocona forma 'if/else'

                    if (attackBonus != 0 || defensiveBonus != 0)
                    {
                        messageManager.ShowMessage($"<color=#00FF9A>{attackerStats.Name}</color> Rzut na US: {wynik} Modyfikator: {attackBonus - defensiveBonus}", 6f);
                        Debug.Log($"{attackerStats.Name} Rzut na US: {wynik} Modyfikator: {attackBonus - defensiveBonus}");
                    }
                    else
                    {
                        messageManager.ShowMessage($"<color=#00FF9A>{attackerStats.Name}</color> Rzut na US: {wynik}", 6f);
                        Debug.Log($"{attackerStats.Name} Rzut na US: {wynik}");
                    }

                    // resetuje naladowanie broni po wykonaniu strzalu
                    attackerStats.reloadLeft = attackerStats.reloadTime;

                    // uwzglednia zdolnosc blyskawicznego przeladowania
                    if (attackerStats.instantReload == true)
                        attackerStats.reloadLeft--;              
                }
                // sprawdza czy atak jest atakiem w zwarciu
                if (attackDistance <= 1.5f || MovementManager.Charge)
                {
                    attackerStats.distanceFight = false;

                    hit = wynik <= attackerStats.WW + attackBonus - defensiveBonus; // zwraca do 'hit' wartosc 'true' jesli to co jest po '=' jest prawda. Jest to skrocona forma 'if/else'

                    if (attackBonus > 0 || defensiveBonus > 0)
                    {
                        messageManager.ShowMessage($"<color=#00FF9A>{attackerStats.Name}</color> Rzut na WW: {wynik} Modyfikator: {attackBonus - defensiveBonus}", 6f);
                        Debug.Log($"{attackerStats.Name} Rzut na WW: {wynik} Modyfikator: {attackBonus - defensiveBonus}");
                    }
                    else
                    {
                        messageManager.ShowMessage($"<color=#00FF9A>{attackerStats.Name}</color> Rzut na WW: {wynik}", 6f);
                        Debug.Log($"{attackerStats.Name} Rzut na WW: {wynik}");
                    }
                }

                //wywołanie funkcji parowania lub uniku jeśli postać jeszcze może to robić w tej rundzie
                if (hit && (attackDistance <= 1.5f || MovementManager.Charge))
                {
                    //sprawdza, czy atakowana postac ma wieksza szanse na unik, czy na parowanie i na tej podstawie ustala kolejnosc tych akcji
                    if (targetStats.WW + targetStats.parryBonus > (targetStats.Zr + (targetStats.Dodge * 10) - 10))
                    {
                        if (targetStats.canParry && !targetStats.distanceFight)
                            ParryAttack(attackerStats, targetStats);
                        else if (targetStats.canDodge)
                            DodgeAttack(targetStats);
                    }
                    else
                    {
                        if (targetStats.canDodge)
                            DodgeAttack(targetStats);
                        else if (targetStats.canParry && !targetStats.distanceFight)
                            ParryAttack(attackerStats, targetStats);
                    }
                }

                // zresetowanie bonusu za celowanie, jeśli jest aktywny
                if (attackerStats.aimingBonus != 0)
                    attackerStats.aimingBonus = 0;
                // Zresetowanie bonusu do trafienia
                attackBonus = 0;

                if (hit && targetDefended != true)
                {
                    int armor = CheckAttackLocalization(targetStats);
                    int damage;
                    int rollResult;

                    // mechanika broni przebijajacej zbroje
                    if ((attackDistance <= 1.5f && attackerStats.PrzebijajacyZbroje || attackDistance > 1.5f && attackerStats.PrzebijajacyZbrojeDystansowa) && armor >= 1)
                        armor--;

                    // mechanika bronii druzgoczacej
                    if (attackDistance <= 1.5f && attackerStats.Druzgoczacy || attackDistance > 1.5f && attackerStats.DruzgoczacyDystansowa)
                    {
                        int roll1 = UnityEngine.Random.Range(1, 11);
                        int roll2 = UnityEngine.Random.Range(1, 11);
                        rollResult = roll1 >= roll2 ? roll1 : roll2;
                        messageManager.ShowMessage($"Atak druzgoczącą bronią. Rzut 1: {roll1} Rzut 2: {roll2}", 6f);
                        Debug.Log($"Atak druzgoczącą bronią. Rzut 1: {roll1} Rzut 2: {roll2}");
                    }
                    else
                        rollResult = UnityEngine.Random.Range(1, 11);

                    // mechanika broni ciezkiej. Czyli po pierszym CELNYM ataku bron traci ceche druzgoczacy. Wg podrecznika traci sie to po pierwszej rundzie, ale wole tak :)
                    if (attackDistance <= 1.5f && attackerStats.Ciezki)
                        attackerStats.Druzgoczacy = false;

                    // mechanika furii ulryka
                    if (rollResult == 10)
                    {
                        int confirmRoll = UnityEngine.Random.Range(1, 101); //rzut na potwierdzenie furii
                        int additionalDamage = 0; //obrazenia ktore dodajemy do wyniku rzutu

                        if (attackDistance <= 1.5f)
                        {
                            if (attackerStats.WW >= confirmRoll)
                            {
                                additionalDamage = UnityEngine.Random.Range(1, 11);
                                rollResult = rollResult + additionalDamage;
                                messageManager.ShowMessage($"Rzut na WW: {confirmRoll}.<color=red> FURIA ULRYKA! </color>", 6f);
                                Debug.Log($"Rzut na potwierdzenie {confirmRoll}. FURIA ULRYKA!");
                            }
                            else
                            {
                                rollResult = 10;
                                messageManager.ShowMessage($"Rzut na WW: {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.", 6f);
                                Debug.Log($"Rzut na potwierdzenie {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.");
                            }
                        }
                        else if (attackDistance > 1.5f)
                        {
                            if (attackerStats.US >= confirmRoll)
                            {
                                additionalDamage = UnityEngine.Random.Range(1, 11);
                                rollResult = rollResult + additionalDamage;
                                messageManager.ShowMessage($"Rzut na US: {confirmRoll}.<color=red> FURIA ULRYKA! </color>", 6f);
                                Debug.Log($"Rzut na potwierdzenie {confirmRoll}. FURIA ULRYKA!");
                            }
                            else
                            {
                                rollResult = 10;
                                messageManager.ShowMessage($"Rzut na US: {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.", 6f);
                                Debug.Log($"Rzut na potwierdzenie {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.");
                            }
                        }

                        while (additionalDamage == 10)
                        {
                            additionalDamage = UnityEngine.Random.Range(1, 11);
                            rollResult = rollResult + additionalDamage;
                            messageManager.ShowMessage($"<color=red> KOLEJNA FURIA ULRYKA! </color>", 6f);
                            Debug.Log($"KOLEJNA FURIA ULRYKA!");
                        }
                    }

                    if (attackDistance <= 1.5f)
                        damage = attackerStats.StrongBlow ? rollResult + attackerStats.Weapon_S + 1 : rollResult + attackerStats.Weapon_S;
                    else
                        damage = attackerStats.PrecisionShot ? rollResult + attackerStats.DistanceWeapon_S + 1 : rollResult + attackerStats.DistanceWeapon_S;

                    messageManager.ShowMessage($"<color=#00FF9A>{attackerStats.Name}</color> wyrzucił {rollResult} i zadał <color=#00FF9A>{damage} obrażeń.</color>", 8f);
                    Debug.Log($"{attackerStats.Name} wyrzucił {rollResult} i zadał {damage} obrażeń.");

                    if (damage > (targetStats.Wt + armor))
                    {
                        targetStats.tempHealth -= (damage - (targetStats.Wt + armor));

                        messageManager.ShowMessage(targetStats.Name + " znegował " + (targetStats.Wt + armor) + " obrażeń.", 8f);
                        Debug.Log(targetStats.Name + " znegował " + (targetStats.Wt + armor) + " obrażeń.");

                        messageManager.ShowMessage($"<color=red> Punkty życia {targetStats.Name}: {targetStats.tempHealth}/{targetStats.maxHealth}</color>", 8f);
                        Debug.Log($"Punkty życia {targetStats.Name}: {targetStats.tempHealth}/{targetStats.maxHealth}");

                        if (targetStats.criticalCondition == false && targetStats.tempHealth < 0)
                        {
                            GameObject.Find("ExpManager").GetComponent<ExpManager>().GainExp(attacker, target); // dodanie expa za pokonanie przeciwnika
                        }
                    }
                    else
                    {
                        messageManager.ShowMessage($"Atak <color=red>{attackerStats.Name}</color> nie przebił się przez pancerz.", 6f);
                        Debug.Log($"Atak {attackerStats.Name} nie przebił się przez pancerz.");
                    }
                }
                else
                {
                    messageManager.ShowMessage($"Atak <color=red>{attackerStats.Name}</color> chybił.", 6f);
                    Debug.Log($"Atak {attackerStats.Name} chybił.");
                }
                targetDefended = false; // przestawienie boola na false, żeby przy kolejnym ataku znowu musiał się bronić, a nie był obroniony na starcie

                // Włączenie animacji ataku
                if (!GameManager.AutoMode)
                    GameObject.Find("AnimationManager").GetComponent<AnimationManager>().AttackAnimation(target.transform.position);        
            }
            else
            {
                messageManager.ShowMessage($"<color=red>Cel ataku stoi poza zasięgiem.</color>", 3f);
                Debug.Log($"Cel ataku stoi poza zasięgiem.");
            }
        }
        while (false);
    }
    #endregion

    #region Check for attack localization function
    public int CheckAttackLocalization(Stats targetStats)
    {
        int attackLocalization = UnityEngine.Random.Range(1, 101);
        int armor = 0;

        switch (attackLocalization)
        {
            case int n when (n >= 1 && n <= 15):
                messageManager.ShowMessage("Trafienie w głowę", 6f);
                Debug.Log("Trafienie w głowę");
                armor = targetStats.PZ_head;
                break;
            case int n when (n >= 16 && n <= 35):
                messageManager.ShowMessage("Trafienie w prawą rękę", 6f);
                Debug.Log("Trafienie w prawą rękę");
                armor = targetStats.PZ_arms;
                break;
            case int n when (n >= 36 && n <= 55):
                messageManager.ShowMessage("Trafienie w lewą rękę", 6f);
                Debug.Log("Trafienie w lewą rękę");
                armor = targetStats.PZ_arms;
                break;
            case int n when (n >= 56 && n <= 80):
                messageManager.ShowMessage("Trafienie w korpus", 6f);
                Debug.Log("Trafienie w korpus");
                armor = targetStats.PZ_torso;
                break;
            case int n when (n >= 81 && n <= 90):
                messageManager.ShowMessage("Trafienie w prawą nogę", 6f);
                Debug.Log("Trafienie w prawą nogę");
                armor = targetStats.PZ_legs;
                break;
            case int n when (n >= 91 && n <= 100):
                messageManager.ShowMessage("Trafienie w lewą nogę", 6f);
                Debug.Log("Trafienie w lewą nogę");
                armor = targetStats.PZ_legs;
                break;
        }
        return armor;

    }
    #endregion

    #region Parry and dodge
    private void ParryAttack(Stats attackerStats, Stats targetStats)
    {
        // sprawdza, czy atakowany ma jakieś bonusy do parowania
        if (targetStats.Parujacy)
            targetStats.parryBonus += 10;
        if (attackerStats.Powolny)
            targetStats.parryBonus += 10;
        if (attackerStats.Szybki)
            targetStats.parryBonus -= 10;

        targetStats.canParry = false;
        int wynik = UnityEngine.Random.Range(1, 101);

        if (targetStats.parryBonus != 0)
        {
            messageManager.ShowMessage($"{targetStats.Name} Rzut na parowanie: {wynik} Bonus do parowania: {targetStats.parryBonus}", 5f);
            Debug.Log($"{targetStats.Name} Rzut na parowanie: {wynik} Bonus do parowania: {targetStats.parryBonus}");
        }
        else
        {
            messageManager.ShowMessage($"{targetStats.Name} Rzut na parowanie: {wynik}", 5f);
            Debug.Log($"{targetStats.Name} Rzut na parowanie: {wynik}");
        }

        if (wynik <= targetStats.WW + targetStats.parryBonus)
            targetDefended = true;
        else
            targetDefended = false;

        // zresetowanie bonusu do parowania
        targetStats.parryBonus = 0;
    }

    private void DodgeAttack(Stats targetStats)
    {
        targetStats.canDodge = false;
        int wynik = UnityEngine.Random.Range(1, 101);

        messageManager.ShowMessage($"{targetStats.Name} Rzut na unik: {wynik}", 5f);
        Debug.Log($"{targetStats.Name} Rzut na unik: {wynik}");

        wynik -= (targetStats.Dodge * 10) - 10;

        if (wynik <= targetStats.Zr)
            targetDefended = true;
        else
            targetDefended = false;
    }
    #endregion

    #region Defensive position
    public void DefensivePosition(GameObject button)
    {
        GameObject character = Character.selectedCharacter;

        if (character.GetComponent<Stats>().defensiveBonus == 0)
        {
            if (character.GetComponent<Stats>().actionsLeft == 2)
                characterManager.TakeDoubleAction(character.GetComponent<Stats>());
            else if (GameManager.StandardMode)
            {
                messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
                Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
                return;
            }

            messageManager.ShowMessage("Pozycja obronna", 3f);
            Debug.Log("Pozycja obronna");

            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            character.GetComponent<Stats>().defensiveBonus = 20;
        }
        else
        {
            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
            character.GetComponent<Stats>().defensiveBonus = 0;
        }
    }
    #endregion

    #region Take aim
    public void TakeAim(GameObject button)
    {
        GameObject character = Character.selectedCharacter;

        if (character.GetComponent<Stats>().aimingBonus == 0)
        {
            if (character.GetComponent<Stats>().actionsLeft > 0)
                characterManager.TakeAction(character.GetComponent<Stats>());
            else if (GameManager.StandardMode)
            {
                messageManager.ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
                Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
                return;
            }

            messageManager.ShowMessage("Przycelowanie", 3f);
            Debug.Log("Przycelowanie");
            character.GetComponent<Stats>().aimingBonus = 10;

            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        }
        else
        {
            character.GetComponent<Stats>().actionsLeft ++;
            character.GetComponent<Stats>().aimingBonus = 0;
            button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
        }
    }
    #endregion

    #region Opportunity attack function
    // Wykonanie ataku okazyjnego
    public void OpportunityAttack(GameObject attacker, GameObject target)
    {
        Stats attackerStats = attacker.GetComponent<Stats>();
        Stats targetStats = target.GetComponent<Stats>();

        int wynik = UnityEngine.Random.Range(1, 101);
        bool hit = false;

        messageManager.ShowMessage($"Ruch spowodował atak okazyjny. Rzut na WW <color=red>{attackerStats.Name}</color>: {wynik}", 6f);
        Debug.Log($"Ruch spowodował atak okazyjny. Rzut na WW {attackerStats.Name}: {wynik}");

        hit = wynik <= attackerStats.WW;  // zwraca do 'hit' wartosc 'true' jesli to co jest po '=' jest prawda. Jest to skrocona forma 'if/else'

        if (hit)
        {
            int armor = CheckAttackLocalization(targetStats);
            int damage;
            int rollResult;

            // mechanika broni przebijajacej zbroje
            if (attackerStats.PrzebijajacyZbroje && armor >= 1)
                armor--;

            // mechanika bronii druzgoczacej
            if (attackerStats.Druzgoczacy)
            {
                int roll1 = UnityEngine.Random.Range(1, 11);
                int roll2 = UnityEngine.Random.Range(1, 11);
                rollResult = roll1 >= roll2 ? roll1 : roll2;
                messageManager.ShowMessage($"Atak druzgoczącą bronią. Rzut 1: {roll1} Rzut 2: {roll2}", 6f);
                Debug.Log($"Atak druzgoczącą bronią. Rzut 1: {roll1} Rzut 2: {roll2}");
            }
            else
                rollResult = UnityEngine.Random.Range(1, 11);

            // mechanika broni ciezkiej. Czyli po pierszym CELNYM ataku bron traci ceche druzgoczacy. Wg podrecznika traci sie to po pierwszej rundzie, ale wole tak :)
            if (attackerStats.Ciezki)
                attackerStats.Druzgoczacy = false;

            // mechanika furii ulryka
            if (rollResult == 10)
            {
                int confirmRoll = UnityEngine.Random.Range(1, 101); //rzut na potwierdzenie furii
                int additionalDamage = 0; //obrazenia ktore dodajemy do wyniku rzutu

                if (attackDistance <= 1.5f)
                {
                    if (attackerStats.WW >= confirmRoll)
                    {
                        additionalDamage = UnityEngine.Random.Range(1, 11);
                        rollResult = rollResult + additionalDamage;
                        messageManager.ShowMessage($"Rzut na WW: {confirmRoll}.<color=red> FURIA ULRYKA! </color>", 6f);
                        Debug.Log($"Rzut na potwierdzenie {confirmRoll}. FURIA ULRYKA!");
                    }
                    else
                    {
                        rollResult = 10;
                        messageManager.ShowMessage($"Rzut na WW: {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.", 6f);
                        Debug.Log($"Rzut na potwierdzenie {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.");
                    }
                }
                else if (attackDistance > 1.5f)
                {
                    if (attackerStats.US >= confirmRoll)
                    {
                        additionalDamage = UnityEngine.Random.Range(1, 11);
                        rollResult = rollResult + additionalDamage;
                        messageManager.ShowMessage($"Rzut na US: {confirmRoll}.<color=red> FURIA ULRYKA! </color>", 6f);
                        Debug.Log($"Rzut na potwierdzenie {confirmRoll}. FURIA ULRYKA!");
                    }
                    else
                    {
                        rollResult = 10;
                        messageManager.ShowMessage($"Rzut na US: {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.", 6f);
                        Debug.Log($"Rzut na potwierdzenie {confirmRoll}. Nie udało się potwierdzić Furii Ulryka.");
                    }
                }

                while (additionalDamage == 10)
                {
                    additionalDamage = UnityEngine.Random.Range(1, 11);
                    rollResult = rollResult + additionalDamage;
                    messageManager.ShowMessage($"<color=red> KOLEJNA FURIA ULRYKA! </color>", 6f);
                    Debug.Log($"KOLEJNA FURIA ULRYKA!");
                }
            }

            damage = rollResult + attackerStats.S;

            messageManager.ShowMessage($"<color=#00FF9A>{attackerStats.Name}</color> wyrzucił {rollResult} i zadał <color=#00FF9A>{damage} obrażeń.</color>", 8f);
            Debug.Log($"{attackerStats.Name} wyrzucił {rollResult} i zadał {damage} obrażeń.");

            if (damage > (targetStats.Wt + armor))
            {
                targetStats.tempHealth -= (damage - (targetStats.Wt + armor));

                messageManager.ShowMessage(targetStats.Name + " znegował " + (targetStats.Wt + armor) + " obrażeń.", 8f);
                Debug.Log(targetStats.Name + " znegował " + (targetStats.Wt + armor) + " obrażeń.");
                messageManager.ShowMessage($"<color=red> Punkty życia {targetStats.Name}: {targetStats.tempHealth}/{targetStats.maxHealth}</color>", 8f);
                Debug.Log($"Punkty życia {targetStats.Name}: {targetStats.tempHealth}/{targetStats.maxHealth}");

                if (targetStats.criticalCondition == false && targetStats.tempHealth < 0)
                {
                    GameObject.Find("ExpManager").GetComponent<ExpManager>().GainExp(attacker, target); // dodanie expa za pokonanie przeciwnika
                }
            }
            else
            {
                messageManager.ShowMessage($"Atak <color=red>{attackerStats.Name}</color> nie przebił się przez pancerz.", 6f);
                Debug.Log($"Atak {attackerStats.Name} nie przebił się przez pancerz.");
            }
        }
        else
        {
            messageManager.ShowMessage($"Atak <color=red>{attackerStats.Name}</color> chybił.", 6f);
            Debug.Log($"Atak {attackerStats.Name} chybił.");
        }
    }
    #endregion

    #region Charge attack function
    public void ChargeAttack(GameObject attacker, GameObject target)
    {
        Stats attackerStats = attacker.GetComponent<Stats>();

        // wektor we wszystkie osiem kierunkow dookola pola z postacia bedaca celem ataku
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, new Vector3(1, 1, 0), new Vector3(-1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, -1, 0) };

        // lista pol przylegajacych do postaci
        List<GameObject> adjacentTiles = new List<GameObject>();

        // Szuka pol w kazdym kierunku
        foreach (Vector3 direction in directions)
        {
            // znajdz wszystkie kolidery w miejscach przylegajacych do postaci bedacej celem ataku
            // jesli w jednym miejscu wystepuje wiecej niz jeden collider to oznacza, ze pole jest zajete przez postac, wtedy nie dodajemy tego collidera do listy adjacentTiles
            Collider2D[] collider = Physics2D.OverlapCircleAll(target.transform.position + direction, 0.1f);

            if (collider != null && collider.Length == 1 && collider[0].CompareTag("Tile") && movementManager.tilesInMovementRange.Contains(collider[0].gameObject))
            {
                adjacentTiles.Add(collider[0].gameObject);
            }
        }

        if (adjacentTiles.Count == 0)
        {
            messageManager.ShowMessage($"<color=red>Cel ataku stoi poza zasięgiem szarży.</color>", 3f);
            Debug.Log($"Cel ataku stoi poza zasięgiem szarży.");
            movementManager.ResetChargeAndRun();
            GameObject.Find("ButtonManager").GetComponent<ButtonManager>().ShowOrHideActionsButtons(attacker, true);
            return;
        }

        // Zamienia liste na tablice, zeby pozniej mozna bylo ja posortowac
        GameObject[] adjacentTilesArray = adjacentTiles.ToArray();

        // Sortuje przylegajace do postaci pola wg odleglosci od atakujacego. Te ktore sa najblizej znajduja sie na poczatku tablicy
        Array.Sort(adjacentTilesArray, (x, y) => Vector3.Distance(x.transform.position, attacker.transform.position).CompareTo(Vector3.Distance(y.transform.position, attacker.transform.position)));

        // Sprawdza dystans do pola docelowego
        Vector3 targetTilePos = new Vector3(adjacentTilesArray[0].transform.position.x, adjacentTilesArray[0].transform.position.y, 0);
        List<Vector3> path = movementManager.FindPath(attacker.transform.position, targetTilePos, attackerStats.Sz * 2);

        if (path.Count >= 3 && path.Count <= attackerStats.Sz * 2) // Jesli jest w zasiegu szarzy
        {
            // Odznacza postać na czas wykonywanania szarży
            attacker.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            GameObject.Find("ActionsButtons").transform.Find("Canvas").gameObject.SetActive(false);
            if (attacker.CompareTag("Player"))
                attacker.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
            else if (attacker.CompareTag("Enemy"))
                attacker.GetComponent<Renderer>().material.color = new Color(255, 0, 0);


            //Wykonanie szarzy
            if (adjacentTilesArray != null)
            {
                attackerStats.tempSz = attackerStats.Sz * 2;

                MovementManager.canMove = true;

                MovementManager.Charge = true;
                movementManager.MoveSelectedCharacter(adjacentTilesArray[0], attacker);
                Physics2D.SyncTransforms(); // Synchronizuje collidery (inaczej Collider2D nie wykrywa zmian pozycji postaci)


                // Wywołanie funkcji z wyczekaniem na koniec animacji ruchu postaci
                StartCoroutine(DelayedAttack(attacker, target, path.Count * 0.2f));

                IEnumerator DelayedAttack(GameObject attacker, GameObject target, float delay)
                {
                    yield return new WaitForSeconds(delay);
                    MovementManager.Charge = true;
                    Attack(attacker, target);
                }

                // Zresetowanie szarzy
                movementManager.ResetChargeAndRun();
            }
        }
        else if (path.Count < 3) // Jesli jest zbyt blisko na szarze
        {
            GameObject.Find("ActionsButtons").transform.Find("Canvas").gameObject.SetActive(true);
            movementManager.ResetChargeAndRun();

            messageManager.ShowMessage($"<color=red>Zbyt mała odległość na wykonanie szarży.</color>", 3f);
            Debug.Log("Zbyt mała odległość na wykonanie szarży");
        }
    }
    #endregion
}