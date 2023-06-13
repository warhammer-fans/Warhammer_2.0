using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; // Potrzebne przy edytowaniu inspektora i szukaniu pliku .json w folderze Resources

public class Stats : MonoBehaviour
{
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

    [Header("Inicjatywa, zdolności, umiejętności, inne")]
    public int Initiative; // inicjatywa
    [HideInInspector] public int Dodge; // poziom umiejętności uniku
    [HideInInspector] public int ChannelingMagic; // poziom umiejętności splatania magii
    [HideInInspector] public bool instantReload; // informacja o tym, czy postać posiada zdolność blyskawicznego przeladowania
    [HideInInspector] public bool QuickDraw; // informacja o tym, czy postac posiada zdolność szybkiego wyciągnięcia
    [HideInInspector] public bool canParry = true; // informacja o tym, czy postac może parować atak
    [HideInInspector] public bool canDodge; // informacja o tym, czy postac może unikać ataku
    [HideInInspector] public bool canSpell = true; // informacja o tym, czy postac może w danej rundzie rzucić zaklęcie
    [HideInInspector] public bool isScary; // informacja o tym, że postać jest Straszna
    [HideInInspector] public bool isScared = true; // informacja o tym, że postać jest przestraszona
    [HideInInspector] public bool Brave; // informacja o tym, czy postać posiada zdolność Odwaga
    [HideInInspector] public bool MagicSense; // informacja o tym, czy postać posiada zdolność Zmysł Magii (+10 splatanie)
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
    public double DistanceAttackRange = 12;
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
    public int Spell_S = 3; // siła zaklęcia
    public int PowerRequired = 6; // wymagany poziom mocy zaklęcia ofensywnego
    public double SpellRange = 8; // zasięg zaklęcia
    public int AreaSize; // wielkość obszaru objętego działaniem zaklęcia
    public int CastDuration = 1; // czas rzucania zaklęcia
    public int CastDurationLeft;
    public int SpellDuration; // czas trwania zaklęcia, obecnie uzywany tylko do pancerzu eteru
    [HideInInspector] public bool OffensiveSpell; // określa, czy zaklęcie jest ofensywne (może byc rzucane tylko na przeciwników)
    public bool IgnoreArmor; // Określa, czy zaklęcie ofensywne ignoruje pancerz
    [HideInInspector] public bool etherArmorActive = false; // Określa, czy postać ma aktywny pancerz eteru



    // Próba wczytania bestiariusza i zaktualizowania wartości cech zgodnie z plikiem .json (NIEUDANA, lista potworow w inspektorze jest pusta)
    /*
    public TextAsset jsonBestiariusz;

    //[System.Serializable]
    public class Bestiariusz
    {
        public Stats[] potwory;
    }

    public Bestiariusz mojBestiariusz = new Bestiariusz();
    public Stats[] potwory;

    void Start()
    {
        jsonBestiariusz = Resources.Load<TextAsset>("Bestiariusz");
        //mojBestiariusz = JsonUtility.FromJson<Bestiariusz>(jsonBestiariusz.text);
        potwory = JsonUtility.FromJson<Stats[]>(jsonBestiariusz.text);

        //potwory = mojBestiariusz.potwory;
    }
    */

    #region Set base stats for Characters by race function
    public void SetBaseStatsByRace(Character.Rasa rasa)
    {
        Rasa = rasa.ToString();

        WW = 20 + Random.Range(2, 21);
        US = 20 + Random.Range(2, 21);
        K = 20 + Random.Range(2, 21);
        Odp = 20 + Random.Range(2, 21);
        Zr = 20 + Random.Range(2, 21);
        Int = 20 + Random.Range(2, 21);
        SW = 20 + Random.Range(2, 21);
        Ogd = 20 + Random.Range(2, 21);
        A = 1;
        Mag = 0;
        Sz = 4;

        int rollMaxHealth = Random.Range(1, 11);
        if (rollMaxHealth <= 3)
            maxHealth = 10;
        else if (rollMaxHealth <= 6)
            maxHealth = 11;
        else if (rollMaxHealth <= 9)
            maxHealth = 12;
        else if (rollMaxHealth == 10)
            maxHealth = 13;

        int rollPP = Random.Range(1, 11);
        if (rollPP <= 4)
            PP = 2;
        else if (rollPP <= 7)
            PP = 3;
        else if (rollPP >= 8)
            PP = 3;

        // ustawienie bazowego zasiegu broni (bron do walki w zwarciu lub łuk)   
        int rollWeaponType = Random.Range(1, 11);
        if (rollWeaponType <= 7)
        {
            distanceFight = false;
            AttackRange = 1.5;
        }
        else
        {
            distanceFight = true;
            AttackRange = DistanceAttackRange;
        }

        // Ustawienie domyślnych statystyk ekwipunku i zdolności, żeby przy zmianie rasy były one resetowane
        PZ_head = 0;
        PZ_arms = 0;
        PZ_torso = 0;
        PZ_legs = 0;

        Druzgoczacy = false;
        Powolny = false;
        PrzebijajacyZbroje = false;
        isScary = false;
        StrongBlow = false;

        if (rasa == Character.Rasa.Elf)
        {
            US += 10;
            Zr += 10;
            maxHealth -= 1;
            Sz = 5;
            PP--;
        }
        else if (rasa == Character.Rasa.Krasnolud)
        {
            WW += 10;
            Odp += 10;
            Zr -= 10;
            Ogd -= 10;
            maxHealth += 1;
            Sz = 3;
            if(PP != 3)
                PP--;
            Brave = true;
        }
        else if (rasa == Character.Rasa.Niziołek)
        {
            WW -= 10;
            US += 10;
            K -= 10;
            Odp -= 10;
            Zr += 10;
            Ogd += 10;
            maxHealth -= 2;
            if (rollPP <= 7 && rollPP > 4)
                PP--;
        }
        else if (rasa == Character.Rasa.Goblin)
        {
            WW -= 6;
            US --;
            K --;
            Odp --;
            Zr -= 6;
            Int -= 6;
            SW --;
            Ogd -= 11;

            maxHealth -= 3;

            PZ_torso = 1;
        }
        else if (rasa == Character.Rasa.Minotaur)
        {
            WW += 11;
            US -= 6;
            K += 17;
            Odp += 15;
            Zr += 7;
            Int -= 13;
            SW -= 7;
            Ogd -= 15;

            A += 1;
            maxHealth += 15;
            Sz = 6;

            PZ_arms = 1;
            PZ_torso = 1;

            AttackRange = 1.5;
            Druzgoczacy = true;
            Powolny = true;
            isScary = true;
            StrongBlow = true;
        }
        else if (rasa == Character.Rasa.Ogr)
        {
            US -= 10;
            K += 15;
            Odp += 14;
            Zr -= 7;
            Int -= 9;
            SW += 4;
            Ogd -= 11;

            A += 2;
            maxHealth += 13;
            Sz = 6;

            PZ_head = 2;
            PZ_arms = 1;
            PZ_torso = 3;
            PZ_legs = 1;

            AttackRange = 1.5;
            isScary = true;
            StrongBlow = true;
        }
        else if (rasa == Character.Rasa.Ork)
        {
            WW += 4;
            US += 4;
            K += 4;
            Odp += 14;
            Zr -= 6;
            Int -= 6;
            SW --;
            Ogd -= 11;

            maxHealth += 1;

            PZ_head = 1;
            PZ_arms = 1;
            PZ_torso = 3;

            StrongBlow = true;
        }
        else if (rasa == Character.Rasa.Skaven)
        {
            WW -= 1;
            US -= 6;
            K -= 1;
            Odp -= 1;
            Zr += 9;
            Int -= 6;
            SW -= 6;
            Ogd -= 16;

            maxHealth -= 2;
            Sz = 5;

            PZ_head = 1;
            PZ_arms = 1;
            PZ_torso = 1;
        }
        else if (rasa == Character.Rasa.Smok)
        {
            WW += 28;
            US = 0;
            K += 34;
            Odp += 37;
            Zr -= 1;
            Int += 16;
            SW += 58;
            Ogd += 3;

            A += 5;
            maxHealth += 44;
            Sz = 6;

            PZ_head = 5;
            PZ_arms = 5;
            PZ_torso = 5;
            PZ_legs = 5;

            AttackRange = 1.5;
            Druzgoczacy = true;
            PrzebijajacyZbroje = true;
            isScary = true;
            StrongBlow = true;
        }
        else if (rasa == Character.Rasa.Troll)
        {
            WW += 6;
            US -= 16;
            K += 20;
            Odp += 17;
            Zr -= 11;
            Int -= 13;
            SW -= 4;
            Ogd -= 21;

            A += 2;
            maxHealth += 19;
            Sz = 6;

            AttackRange = 1.5;
            Druzgoczacy = true;
            Powolny = true;
            isScary = true;
        }
        else if (rasa == Character.Rasa.Zwierzoczłek)
        {
            WW += 9;
            US -= 6;
            K += 4;
            Odp += 13;
            Zr += 4;
            Int -= 5;
            SW -= 6;
            Ogd -= 6;

            maxHealth += 1;
            Sz = 5;

            PZ_arms = 1;
            PZ_torso = 1;
        }

        Initiative = Zr + Random.Range(1, 11);
        S = Mathf.RoundToInt(K / 10);
        Wt = Mathf.RoundToInt(Odp / 10);
        tempHealth = maxHealth;
        tempSz = Sz;
        attacksLeft = A;
        CastDurationLeft = CastDuration;
        Weapon_S = S;
        DistanceWeapon_S = 3;
    }
    #endregion
}

