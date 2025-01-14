using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitDetail : MonoBehaviour
{
    private static Unit selectedUnit;

    [SerializeField]
    Image backgroundImage;

    [SerializeField]
    Image selectedCharacterImage;

    [SerializeField]
    GameObject characterNameContainer;

    [SerializeField]
    GameObject errorPopUp;

    [SerializeField]
    TextMeshProUGUI errorPopUpText;

    [SerializeField]
    GameObject needToTierUpPopup;

    [SerializeField]
    List<UIEquipmentSlot> equipmentSlots;

    [SerializeField]
    TMP_Text unitLevelText;

    [SerializeField]
    TMP_Text unitTierText;

    [SerializeField]
    TMP_Text levelUpGoldCostText;

    [SerializeField]
    TMP_Text tierUpGemsCostText;

    [SerializeField]
    TMP_Text tierUpGoldCostText;

    [SerializeField]
    AudioSource levelUpSound;

    void Start()
    {
        SetUpEquipment();
        SetBackgroundImage();
        DisplayUnit();
    }

    public void LevelUp()
    {
        SocketConnection.Instance.LevelUpUnit(GlobalUserData.Instance.User.id, selectedUnit.id,
        (unitAndCurrencies) =>
        {
            UpdateUserCurrenciesAndCosts(unitAndCurrencies);
        },
        (reason) =>
        {
            switch (reason)
            {
                case "cant_afford":
                    errorPopUpText.text = "Not enough currency";
                    errorPopUp.SetActive(true);
                    break;
                case "cant_level_up":
                    tierUpGoldCostText.text = ((int)Math.Pow(selectedUnit.level, 2)).ToString();
                    tierUpGemsCostText.text = "50";
                    needToTierUpPopup.SetActive(true);
                    break;
                default:
                    Debug.LogError(reason);
                    break;
            }
        });
    }

    public void TierUp()
    {
        SocketConnection.Instance.TierUpUnit(GlobalUserData.Instance.User.id, selectedUnit.id,
        (unitAndCurrencies) =>
        {
            UpdateUserCurrenciesAndCosts(unitAndCurrencies);
        },
        (reason) =>
        {
            switch (reason)
            {
                case "cant_afford":
                    errorPopUpText.text = "Not enough currency";
                    errorPopUp.SetActive(true);
                    break;
                case "cant_tier_up":
                    errorPopUpText.text = "Need to rank up first";
                    errorPopUp.SetActive(true);
                    break;
                default:
                    Debug.LogError(reason);
                    break;
            }
        });
    }


    // I think both SelectUnit and GetSelectedUnit should be removed and the selectedUnit field be made public
    public static void SelectUnit(Unit unit)
    {
        selectedUnit = unit;
    }

    public static Unit GetSelectedUnit()
    {
        return selectedUnit;
    }

    public void EquipItem(string itemId, string unitId)
    {
        SocketConnection.Instance.EquipItem(GlobalUserData.Instance.User.id, itemId, unitId, (item) =>
        {
            UIEquipmentSlot.selctedEquipmentSlot.SetEquippedItem(item);
            // Should this be encapsulated somewhere?
            GlobalUserData.Instance.User.items.Find(item => item.id == itemId).unitId = unitId;
        });
    }

    public void UnequipItem(string itemId)
    {
        SocketConnection.Instance.UnequipItem(GlobalUserData.Instance.User.id, itemId, (item) =>
        {
            UIEquipmentSlot.selctedEquipmentSlot.SetEquippedItem(null);
            // Should this be encapsulated somewhere?
            GlobalUserData.Instance.User.items.Find(item => item.id == itemId).unitId = null;
        });
    }

    public void LevelUpItem(Item item, Action<Item> onItemDataReceived)
    {
        // Hardcoded to check for gold
        if (item.GetLevelUpCost() > GlobalUserData.Instance.GetCurrency("Gold"))
        {
            errorPopUp.SetActive(true);
            return;
        }
        SocketConnection.Instance.LevelUpItem(GlobalUserData.Instance.User.id, item.id, (item) =>
        {
            onItemDataReceived?.Invoke(item);
        }, (reason) =>
        {
            if (reason == "cant_afford")
            {
                errorPopUp.SetActive(true);
            }
        });
    }

    private void SetUpEquipment()
    {
        foreach (Item item in GlobalUserData.Instance.User.items.Where(item => item.unitId == selectedUnit.id))
        {
            equipmentSlots.Find(slot => slot.EquipmentType == item.template.type).SetEquippedItem(item);
        }
    }

    private void SetBackgroundImage()
    {
        switch (selectedUnit.character.faction)
        {
            case Faction.Araban:
                backgroundImage.sprite = Resources.Load<Sprite>("UI/UnitDetailBackgrounds/ArabanBackground");
                break;
            case Faction.Kaline:
                backgroundImage.sprite = Resources.Load<Sprite>("UI/UnitDetailBackgrounds/KalineBackground");
                break;
            case Faction.Merliot:
                backgroundImage.sprite = Resources.Load<Sprite>("UI/UnitDetailBackgrounds/MerliotBackground");
                break;
            case Faction.Otobi:
                backgroundImage.sprite = Resources.Load<Sprite>("UI/UnitDetailBackgrounds/OtobiBackground");
                break;
            default:
                backgroundImage.sprite = Resources.Load<Sprite>("UI/UnitDetailBackgrounds/ArabanBackground");
                break;
        }
    }

    private void DisplayUnit()
    {
        selectedCharacterImage.sprite = selectedUnit.character.inGameSprite;
        selectedCharacterImage.transform.parent.gameObject.SetActive(true);
        characterNameContainer.GetComponentInChildren<TextMeshProUGUI>().text = selectedUnit.character.name;
        characterNameContainer.SetActive(true);
        unitLevelText.text = $"Level: {selectedUnit.level}";
        unitTierText.text = $"Tier: {selectedUnit.tier}";
        levelUpGoldCostText.text = ((int)Math.Pow(selectedUnit.level, 2)).ToString();
    }

    public void PreviousUnit()
    {
        List<Unit> userUnits = GlobalUserData.Instance.User.units;
        int currentIndex = userUnits.IndexOf(selectedUnit);

        int previousIndex = currentIndex - 1;
        if (previousIndex < 0)
        {
            previousIndex = userUnits.Count - 1;
        }

        Unit previousUnit = userUnits[previousIndex];
        SelectUnit(previousUnit);
    }

    public void NextUnit()
    {
        List<Unit> userUnits = GlobalUserData.Instance.User.units;
        int currentIndex = userUnits.IndexOf(selectedUnit);

        int nextIndex = currentIndex + 1;
        if (nextIndex >= userUnits.Count)
        {
            nextIndex = 0;
        }

        Unit nextUnit = userUnits[nextIndex];
        SelectUnit(nextUnit);
    }

    private void UpdateUserCurrenciesAndCosts(Protobuf.Messages.UnitAndCurrencies unitAndCurrencies)
    {
        foreach (var userCurrency in unitAndCurrencies.UserCurrency)
        {
            GlobalUserData.Instance.SetCurrencyAmount(userCurrency.Currency.Name, (int)userCurrency.Amount);
        }
        levelUpSound.Play();
        GlobalUserData.Instance.User.units.Find(unit => unit.id == unitAndCurrencies.Unit.Id).level = (int)unitAndCurrencies.Unit.Level;

        unitLevelText.text = $"Level: {unitAndCurrencies.Unit.Level}";
        unitTierText.text = $"Tier: {unitAndCurrencies.Unit.Tier}";
        levelUpGoldCostText.text = ((int)Math.Pow(unitAndCurrencies.Unit.Level, 2)).ToString();
    }
}
