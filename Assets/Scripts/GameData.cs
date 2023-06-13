using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class GameData
{
    #region All fields
    public int NumberOfRounds;
    public int gridWidth;
    public int gridHeight;

    [Header("Imię")]
    public string Name;

    [Header("Rasa")]
    public string Rasa;

    public int Level;
    public int Exp;

    [Header("Cechy pierwszorzędowe")]
    public int WW;
    public int US;
    public int K;
    public int Odp;
    public int Zr;
    public int Int;
    public int SW;
    public int Ogd;

    [Header("Cechy drugorzędowe")]
    public int A;
    public int S;
    public int Wt;
    public int Sz;
    [HideInInspector] public int tempSz;
    public int Mag;
    public int maxHealth;
    public int tempHealth;
    public int PP;
    public int PS;

    [Header("Inicjatywa, parowanie, uniki")]
    public int Initiative; // inicjatywa
    [HideInInspector] public int Dodge; // poziom umiejętności uniku
    [HideInInspector] public int ChannelingMagic; // poziom umiejętności splatania magii
    public bool instantReload; // informacja o tym, czy postać posiada zdolność blyskawicznego przeladowania
    [HideInInspector] public bool QuickDraw; // informacja o tym, czy postac posiada zdolność szybkiego wyciągnięcia
    [HideInInspector] public bool canParry = true; // informacja o tym, czy postac może parować atak
    [HideInInspector] public bool canDodge; // informacja o tym, czy postac może unikać ataku
    [HideInInspector] public bool canSpell = true; // informacja o tym, czy postac może w danej rundzie rzucić zaklęcie
    [HideInInspector] public bool isScary; // informacja o tym, że postać jest Straszna
    [HideInInspector] public bool isScared; // informacja o tym, że postać jest przestraszona
    [HideInInspector] public bool Brave; // informacja o tym, czy postać posiada zdolność Odwaga
    [HideInInspector] public bool MagicSense; // informacja o tym, czy postać posiada zdolność Zmysł Magii
    [HideInInspector] public bool StrongBlow; // informacja o tym, czy postać posiada zdolność Silny Cios (+1 obr.)
    [HideInInspector] public bool PrecisionShot; // informacja o tym, czy postać posiada zdolność Strzał Precyzyjny (+1 obr.)
    [HideInInspector] public int actionsLeft = 2; // akcje do wykorzystania w aktualnej rundzie walki
    [HideInInspector] public int attacksLeft; // ilość ataków pozostałych do wykonania w danej rundzie
    [HideInInspector] public bool criticalCondition = false; // sprawdza czy życie postaci jest poniżej 0
    [HideInInspector] public int parryBonus; // sumaryczna premia do WW przy parowaniu
    [HideInInspector] public int defensiveBonus; // premia za pozycje obronna
    [HideInInspector] public int aimingBonus; // premia za przycelowanie

    [Header("Broń")]
    public bool distanceFight;
    public int Weapon_S; // Siła broni
    public int DistanceWeapon_S; // Siła broni dystansowej
    public double AttackRange;
    public double DistanceAttackRange = 15;
    public int reloadTime = 1;
    public int reloadLeft;
    public bool Ciezki;
    public bool Druzgoczacy;
    public bool DruzgoczacyDystansowa;
    public bool Parujacy;
    public bool Powolny;
    public bool PrzebijajacyZbroje;
    public bool PrzebijajacyZbrojeDystansowa;
    public bool Szybki;

    [Header("Punkty zbroi")]
    public int PZ_head;
    public int PZ_arms;
    public int PZ_torso;
    public int PZ_legs;

    [Header("Statystyki zaklęć")]
    public int Spell_S;
    public int PowerRequired;
    public double SpellRange;
    public int AreaSize;
    public int CastDuration;
    public int CastDurationLeft;
    public int SpellDuration; // czas trwania zaklęcia, obecnie uzywany tylko do pancerzu eteru
    public bool OffensiveSpell;
    public bool IgnoreArmor;
    [HideInInspector] public bool etherArmorActive = false; // Określa, czy postać ma aktywny pancerz eteru

    public float[] position;

    public List<float[]> obstaclePositions;
    public List<string> tags;
    #endregion

    // Przypisanie wszystkim polom wartości z konkretnej instancji klasy Stats, a także zapisanie numery rundy
    public GameData(Stats stats)
    {
        NumberOfRounds = RoundManager.roundNumber;

        gridWidth = GridManager.width;
        gridHeight = GridManager.height;

       // INFORMACJE O PRZESZKODACH
       string[] tagsToSave = {"Tree", "Rock", "Wall", "Water"};

        // Pobieramy wszystkie obiekty o wybranych tagach
        List<GameObject> objectsToSave = new List<GameObject>();
        foreach (string tag in tagsToSave)
        {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            objectsToSave.AddRange(objectsWithTag);
        }

        // Tworzymy listę pozycji i tagów obiektów
        obstaclePositions = new List<float[]>();
        tags = new List<string>();
        foreach (GameObject obj in objectsToSave)
        {
            float[] positionArray = new float[3] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z };
            obstaclePositions.Add(positionArray);
            tags.Add(obj.tag);
        }


        // Pobiera wszystkie pola (zmienne) z klasy Stats
        var fields = stats.GetType().GetFields();
        var thisFields = this.GetType().GetFields();

        // Dla każdego pola z klasy stats odnajduje pole w klasie this (czyli GameData) i ustawia mu wartość jego odpowiednika z klasy stats
        foreach (var thisField in thisFields)
        {
            var field = fields.FirstOrDefault(f => f.Name == thisField.Name); // Znajduje pierwsze pole o tej samej nazwie wsród pol z klasy Stats

            if (field != null && field.GetValue(stats) != null)
            {
                thisField.SetValue(this, field.GetValue(stats));
            }
        }

        position = new float[3];
        position[0] = stats.gameObject.transform.position.x;
        position[1] = stats.gameObject.transform.position.y;
        position[2] = stats.gameObject.transform.position.z;
    }

}
