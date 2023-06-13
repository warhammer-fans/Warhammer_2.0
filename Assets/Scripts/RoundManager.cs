using UnityEngine;
using TMPro;

public class RoundManager : MonoBehaviour
{
    public static int roundNumber;
    public AutoCombat autoCombat;

    [SerializeField] private TMP_Text roundNumberDisplay;
    [SerializeField] private TMP_Text nextRoundButtonText;

    private CharacterManager characterManager;

    void Start()
    {
        roundNumber = 0;
        autoCombat = autoCombat.gameObject.GetComponent<AutoCombat>();

        // Odniesienie do menadżera postaci
        characterManager = GameObject.Find("CharacterManager").GetComponent<CharacterManager>();
    }

    void Update()
    {
        if (roundNumber == 0)
        {
            roundNumberDisplay.text = "Gotów?";
            nextRoundButtonText.text = "Start";
        }
        else
        {
            roundNumberDisplay.text = "Runda: " + roundNumber;
            nextRoundButtonText.text = "+";
        }
    }

    public void NextRound()
    {
        if (GameManager.AutoMode)
            autoCombat.AutomaticActions();
      
        Stats[] characters = FindObjectsOfType<Stats>();
        foreach (var character in characters)
        {
            // Ponowne usunięcie postaci, które są w stanie krytycznym, dlatego że wewnątrz AutoCombatu z jakiegoś powodu nie zawsze to działa dobrze
            if (character.tempHealth < 0 && GameManager.AutoMode)
                Destroy(character.gameObject);

            // Zresetowanie pancerzu eteru po odpowiednim czasie
            if (character.SpellDuration > 0)
                character.SpellDuration--;
            if (character.SpellDuration == 0 && character.etherArmorActive)
                GameObject.Find("MagicManager").GetComponent<MagicManager>().EtherArmorSpell(character.gameObject);
        } 

        roundNumber++;

        Stats[] allObjectsWithStats = FindObjectsOfType<Stats>();

        bool scaryEnemyExist = false;

        foreach (Stats obj in allObjectsWithStats)
        {
            characterManager.ResetParryAndDodge(obj.GetComponent<Stats>());
            characterManager.ResetActionsNumber(obj.GetComponent<Stats>());
            characterManager.ResetSpellcastingLimit(obj.GetComponent<Stats>());

            if (obj.isScary)
                scaryEnemyExist = true;
        }

        if(allObjectsWithStats.Length > 0)
            GameObject.Find("CharacterManager").GetComponent<CharacterManager>().SelectCharacterWithBiggestInitiative(scaryEnemyExist, allObjectsWithStats);

        GameObject.Find("MessageManager").GetComponent<MessageManager>().ShowMessage($"<color=#FFE100>RUNDA {roundNumber}</color>", 3f);
        Debug.Log($"======================= RUNDA {roundNumber} =======================");
    }

    public void EndSelectedCharacterTurn()
    {
        Character.selectedCharacter.GetComponent<Stats>().actionsLeft = 0;
        AttackManager.targetSelecting = false;
        MagicManager.targetSelecting = false;
    }
}
