using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TMPro;
using System;

public class StatsEditor : MonoBehaviour
{
    public GameObject generalPanel; // Panel w ktorym wybieramy, jaki typ cech bedziemy chcieli zmieniac

    [SerializeField] TMP_InputField charNameText; // Napis przedstawiajacy na panelu imie postaci

    [SerializeField] TMP_Dropdown rasaDropdown; // Dropdown z wyborem rasy

    [SerializeField] GameObject distanceWeaponStatsPanel;
    [SerializeField] GameObject meleeWeaponStatsPanel;

    #region Panels visibility functions
    public void PanelVisibility(GameObject panel)
    {
        panel.SetActive(true);
        generalPanel.SetActive(false);
    }

    public void ShowGeneralPanel()
    {
        // Znajduje wszystkie panele poza głownym
        GameObject[] otherPanels = GameObject.FindGameObjectsWithTag("Panel");

        // Zmiana wysietlanego w panelu imienia na imie wybranej postaci
        GameObject character = Character.selectedCharacter;
        charNameText.text = character.GetComponent<Stats>().Name;

        // Zamyka wszystkie inne panele poza glownym
        foreach (var otherPanel in otherPanels)
        {
            otherPanel.SetActive(false);
        }

        // Otwiera lub zamyka glowny panel
        if(!generalPanel.activeSelf)
        {
            GameManager.PanelIsOpen = true;
            generalPanel.SetActive(true);
        }
        else
        {
            generalPanel.SetActive(false);
            GameManager.PanelIsOpen = false;
        }

        SetRaceDropdown();
    }

    public void HideGeneralPanel()
    {
        if (generalPanel.activeSelf)
        {
            generalPanel.SetActive(false);
            GameManager.PanelIsOpen = false;
        }
    }
    #endregion

    #region Load all attributes
    public void LoadAttributes()
    {

        GameObject character = Character.selectedCharacter;

        // Zmiana wysietlanego w panelu imienia na imie wybranej postaci
        charNameText.text = character.GetComponent<Stats>().Name;

        SetRaceDropdown();

        // Wyszukuje wszystkie pola tekstowe i przyciski do ustalania statystyk postaci wewnatrz gry
        GameObject[] inputFields = GameObject.FindGameObjectsWithTag("StatsButton");

        foreach (var inputField in inputFields)
        {
            // Pobiera pole ze statystyk postaci o nazwie takiej jak nazwa textInputa
            FieldInfo field = character.GetComponent<Stats>().GetType().GetField(inputField.name);

            // Jeśli znajdzie takie pole w to zmienia wartość wyswietlanego tekstu na wartosc cechy
            if (field != null && field.FieldType == typeof(int)) // to dziala dla cech opisywanych wartosciami int
            {
                int value = (int)field.GetValue(character.GetComponent<Stats>());

                if (inputField.GetComponent<TMPro.TMP_InputField>() != null)
                    inputField.GetComponent<TMPro.TMP_InputField>().text = value.ToString();
                else if (inputField.GetComponent<Slider>() != null)
                    inputField.GetComponent<Slider>().value = value;
            }
            else if (field != null && field.FieldType == typeof(bool)) // to dziala dla cech opisywanych wartosciami bool
            {
                bool value = (bool)field.GetValue(character.GetComponent<Stats>());
                inputField.GetComponent<Toggle>().isOn = value;
            }
            else if (field != null && field.FieldType == typeof(double)) // to dziala dla cech opisywanych wartosciami float
            {
                double value = (double)field.GetValue(character.GetComponent<Stats>());
                if(value > 1.5f)
                    inputField.GetComponent<TMPro.TMP_InputField>().text = (value*2).ToString(); // przemnaza x2 zeby podac zasieg w metrach a nie polach
                else
                    inputField.GetComponent<TMPro.TMP_InputField>().text = "1"; // gdy jest to bron do walki w zwarciu to wyswietla zasieg rowny 1
            }
        }
 
    }
    #endregion

    #region Edit attribute
    public void EditAttribute(GameObject textInput)
    {
        GameObject character = Character.selectedCharacter;

        // Pobiera pole o nazwie takiej jak nazwa textInputa
        FieldInfo field = character.GetComponent<Stats>().GetType().GetField(textInput.name);

        // Jezeli znajdzie to zmienia wartosc cechy
        if (field != null && field.FieldType == typeof(int))
        {
            // Sprawdza, czy textInput zawiera komponent TMP_text. Jeśli tak to zamienia wprowadzony tekst na int. Jeżeli nie to znaczy, że użyłem slidera, więc pobiera jego wartość.
            int value = (textInput.GetComponent<TMP_InputField>() != null) ?
                        (int.TryParse(textInput.GetComponent<TMP_InputField>().text, out int tempValue) ? tempValue : 0) :
                        (int)textInput.GetComponent<Slider>().value;

            if (value <= 0 && textInput.name != "Weapon_S" && textInput.name != "DistanceWeapon_S" && textInput.name != "Dodge" && textInput.name != "ChannelingMagic")
            {
                value = 0;
                if(textInput.name != "A" && textInput.name != "tempHealth" && textInput.name != "AreaSize")
                    textInput.GetComponent<TMP_InputField>().text = "0";
                else if (textInput.name == "A")
                    textInput.GetComponent<TMP_InputField>().text = "1";
            }
            if(textInput.name == "CastDuration")
                character.GetComponent<Stats>().CastDurationLeft = value;

            field.SetValue(character.GetComponent<Stats>(), value);
            Debug.Log($"Atrybut {field.Name} zmieniony na {value}");

            // Odświeżenie buttonów magii
            if (textInput.name == "Mag")
                GameObject.Find("ButtonManager").GetComponent<ButtonManager>().ShowOrHideMagicButtons(Character.selectedCharacter);
        }
        else if (field != null && field.FieldType == typeof(bool)) 
        {
            bool boolValue = textInput.GetComponent<Toggle>().isOn; // Jezeli znajdzie to zmienia wartosc cechy na true lub false w zależności od wartości Toggle
            field.SetValue(character.GetComponent<Stats>(), boolValue);
            Debug.Log($"Atrybut {field.Name} zmieniony na {boolValue}");
        }
        else if (field != null && field.FieldType == typeof(double)) // to dziala dla cech opisywanych wartosciami float
        {
            double.TryParse(textInput.GetComponent<TMPro.TMP_InputField>().text, out double value); // Zamienia wprowadzony text na wartość float
            if(value > 3)
                field.SetValue(character.GetComponent<Stats>(), value/2); // dzieli wartosc na 2, zeby ustawic zasieg w polach a nie metrach
            else
                field.SetValue(character.GetComponent<Stats>(), 1.5f); // gdy ktos poda zasieg mniejszy niz 3 metry to ustawia domyslna wartosc zasiegu do walki wrecz

            Debug.Log($"Atrybut {field.Name} zmieniony na {value}");
        }
        else if (field != null && field.FieldType == typeof(string)) // to dziala dla cech opisywanych wartosciami string
        {
            string stringValue = textInput.GetComponent<TMPro.TMP_InputField>().text;
            field.SetValue(character.GetComponent<Stats>(), stringValue);
            Debug.Log($"Atrybut {field.Name} zmieniony na {stringValue}");
        }
        else
            Debug.Log($"Nie udało się zmienić wartości cechy.");

        // Aktualizuje poziom postaci
        GameObject.Find("ExpManager").GetComponent<ExpManager>().SetCharacterLevel(character);

        if (character.GetComponent<Stats>().A > 1)
            character.GetComponent<Stats>().attacksLeft = character.GetComponent<Stats>().A;

        LoadAttributes();
    }
    #endregion

    #region Set initiative
    public void SetInitiative()
    {
        GameObject character = Character.selectedCharacter;
        character.GetComponent<Stats>().Initiative = character.GetComponent<Stats>().Zr + UnityEngine.Random.Range(1,11);
        
        Debug.Log($"Inicjatywa zmieniony na {character.GetComponent<Stats>().Initiative}");
    }
    #endregion

    // Kod odpowiadajacy za dropdown do wyboru rasy postaci
    #region Set race dropdown
    void SetRaceDropdown()
    {
        GameObject character = Character.selectedCharacter;

        // Resetuje opcje dropdownu
        rasaDropdown.ClearOptions();

        List<string> options = new List<string>();

        // Tworzy listę dostępnych ras i dodaje ją do opcji dropdownu
        if (character.CompareTag("Player"))
        {
            foreach (Character.Rasa rasa in Enum.GetValues(typeof(Character.Rasa)))
            { 
                if ((int)rasa > 3) break; // przerwij pętlę po czterech pierwszych wartościach enumeratora
                options.Add(rasa.ToString());
            }

            rasaDropdown.AddOptions(options);
            // Ustawia wartość dropdownu na aktualną rasę postaci
            rasaDropdown.value = (int)character.GetComponent<Character>().rasa;
        }
        else if (character.CompareTag("Enemy"))
        {
            foreach (Character.Rasa rasa in Enum.GetValues(typeof(Character.Rasa)))
            { 
                if ((int)rasa > 3) // pomija pierwsze cztery wartości enumeratora
                    options.Add(rasa.ToString());
            }

            rasaDropdown.AddOptions(options);
            // Ustawia wartość dropdownu na aktualną rasę postaci
            rasaDropdown.value = (int)character.GetComponent<Character>().rasa - 4;
        }
    }
    #endregion

    #region Change race
    public void ChangeRace()
    {
        GameObject character = Character.selectedCharacter;
        
        // Jeżeli nie doszło do zmiany rasy to przerwij funkcję
        if (character.GetComponent<Character>().rasa == (Character.Rasa)rasaDropdown.value || character.GetComponent<Character>().rasa == (Character.Rasa)rasaDropdown.value + 4)
            return;

        if (character.CompareTag("Player"))
                character.GetComponent<Character>().rasa = (Character.Rasa)rasaDropdown.value;
        else if (character.CompareTag("Enemy"))
        {
            character.GetComponent<Stats>().isScary = false;
            character.GetComponent<Character>().rasa = (Character.Rasa)rasaDropdown.value + 4;
        }

        character.GetComponent<Stats>().SetBaseStatsByRace(character.GetComponent<Character>().rasa);

        LoadAttributes();

        Debug.Log($"Rasa zmieniona na {character.GetComponent<Character>().rasa}. Statystyki postaci zostały zresetowane.");

        GameObject.Find("ExpManager").GetComponent<ExpManager>().SetCharacterLevel(character);      
    }
    #endregion

    #region Change weapon
    public void ChangeWeapon(GameObject button)
    {
        Stats charStats = Character.selectedCharacter.GetComponent<Stats>();

        if (charStats.actionsLeft > 0 && !charStats.QuickDraw)
            GameObject.Find("CharacterManager").GetComponent<CharacterManager>().TakeAction(charStats);
        else if (GameManager.StandardMode && !charStats.QuickDraw)
        {
            GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage($"<color=red>Postać nie może wykonać tylu akcji w tej rundzie.</color>", 3f);
            Debug.Log($"Postać nie może wykonać tylu akcji w tej rundzie.");
            return;
        }

        button.GetComponent<Image>().color = new Color(0f, 1f, 0f, 0.5f);

        if (button.name == "melee_attack_button")
        {
            charStats.distanceFight = false;
            charStats.AttackRange = 1.5f;
            GameObject.Find("distance_attack_button").GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }
        if (button.name == "distance_attack_button")
        {
            charStats.distanceFight = true;
            charStats.AttackRange = charStats.DistanceAttackRange;
            GameObject.Find("melee_attack_button").GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }

        GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage($"{charStats.Name} zmienił/a broń", 3f);
        Debug.Log($"{charStats.Name} zmienił/a broń");

        // Odświeżenie buttonów akcji
        GameObject.Find("ButtonManager").GetComponent<ButtonManager>().ShowOrHideActionsButtons(Character.selectedCharacter, false);
        GameObject.Find("ButtonManager").GetComponent<ButtonManager>().ShowOrHideActionsButtons(Character.selectedCharacter, true);

        LoadAttributes();
        if(distanceWeaponStatsPanel.activeSelf || meleeWeaponStatsPanel.activeSelf)
            DisplayEquippedWeaponStats();
    }
    #endregion

    #region Display equipped weapon stats
    public void DisplayEquippedWeaponStats()
    {
        if(Character.selectedCharacter.GetComponent<Stats>().distanceFight)
        {
            distanceWeaponStatsPanel.SetActive(true);
            meleeWeaponStatsPanel.SetActive(false);
        }
        else
        {
            meleeWeaponStatsPanel.SetActive(true);
            distanceWeaponStatsPanel.SetActive(false);
        }
    }

    #endregion
}
