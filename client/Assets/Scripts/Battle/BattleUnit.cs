using DG.Tweening;
using DuloGames.UI;
using UnityEngine;
using UnityEngine.UI;
using DuloGames.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class BattleUnit : MonoBehaviour
{
    [SerializeField]
    Image unitImage;

    [SerializeField]
    UIProgressBar healthBar;

    [SerializeField]
    UIProgressBar energyBar;

    [SerializeField]
    GameObject indicatorPrefab;

    [SerializeField]
    CanvasGroup canvasGroup;

    [SerializeField]
    StatusIndicators statusIndicators;

    [SerializeField]
    AudioSource AttackSFX;

    [SerializeField]
    bool isPlayerTeam;
    public bool IsPlayerTeam
    {
        get { return isPlayerTeam; }
    }

    private Unit selectedUnit;
    public Unit SelectedUnit
    {
        get { return selectedUnit; }
        private set { selectedUnit = value; }
    }

    private int maxHealth;
    public int MaxHealth
    {
        get { return maxHealth; }
        set
        {
            if (value != maxHealth)
            {
                maxHealth = value;
                healthBar.fillAmount = currentHealth / (float)maxHealth;
            }
        }
    }

    private int currentHealth;
    public int CurrentHealth
    {
        get { return currentHealth; }
        set
        {
            if (value != currentHealth)
            {
                if (currentHealth > 0)
                {
                    DisplayHealthIndicator(value);
                }
                currentHealth = value;
                healthBar.fillAmount = currentHealth / (float)maxHealth;
            }
        }
    }
    private int maxEnergy;
    public int MaxEnergy
    {
        get { return maxEnergy; }
        set
        {
            if (value != maxEnergy)
            {
                maxEnergy = value;
                energyBar.fillAmount = currentEnergy / (float)maxEnergy;
            }
        }
    }

    private int currentEnergy;
    public int CurrentEnergy
    {
        get { return currentEnergy; }
        set
        {
            if (value != currentEnergy)
            {
                currentEnergy = value;
                energyBar.fillAmount = currentEnergy / (float)maxEnergy;
            }
        }
    }

    public void SetUnit(Unit unit, bool isPlayer)
    {
        selectedUnit = unit;
        GetComponent<Button>().interactable = isPlayer;
        unitImage.sprite = selectedUnit.character.inGameSprite;
        unitImage.gameObject.SetActive(true);
    }

    public void AttackTrigger()
    {
        AttackSFX.Play();
    }
    public void DeathFeedback()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(unitImage.transform.DORotate(new Vector3(0, 180, 90), .5f));
        sequence.Append(unitImage.DOFade(0, 2f));
        sequence.Join(canvasGroup.DOFade(0, 2f));
        sequence.Play();
    }

    private void DisplayHealthIndicator(int value)
    {
        GameObject indicatorGO = Instantiate(indicatorPrefab, gameObject.transform);
        BattleIndicator indicator = indicatorGO.GetComponent<BattleIndicator>();
        indicator.SetText((currentHealth - value).ToString());
        indicator.FadeAnimation();
    }

    public void ApplyStatus(Status status)
    {
        statusIndicators.SetStatus(status);
    }

    public void RemoveStatus(Status status)
    {
        statusIndicators.RemoveStatus(status);
    }
}
