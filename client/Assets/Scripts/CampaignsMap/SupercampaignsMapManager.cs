using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SupercampaignsMapManager : MonoBehaviour
{
    [SerializeField]
    SceneNavigator sceneNavigator;

    [SerializeField]
    AddressableInstantiator addressableInstantiator;

    [SerializeField]
    GameObject supercampaignContainer;

    private GameObject supercampaignInstance;
    public static string selectedSuperCampaignName;

    async void Start()
    {
        await InstantiateSupercampaign();
        SocketConnection.Instance.GetCampaigns(GlobalUserData.Instance.User.id, selectedSuperCampaignName, (campaigns) =>
        {
            // this needs to be refactored, the campaigns have two parallel "paths" that do different things, they should be unified into the static class
            LevelProgress.campaigns = campaigns;
            List<CampaignItem> campaignItems = new List<CampaignItem>(supercampaignInstance.GetComponentsInChildren<CampaignItem>());
            GenerateCampaigns(campaigns, campaignItems);
        });
    }

    private void GenerateCampaigns(List<Campaign> campaigns, List<CampaignItem> campaignItems)
    {
        for (int campaignsIndex = 0; campaignsIndex < campaigns.Count; campaignsIndex++)
        {
            campaignItems[campaignsIndex].sceneNavigator = sceneNavigator;
            campaignItems[campaignsIndex].SetCampaignData(campaigns[campaignsIndex]);
        }
    }

    private async Task InstantiateSupercampaign()
    {
        if (selectedSuperCampaignName == "Main Campaign")
        {
            supercampaignInstance = await addressableInstantiator.InstantiateMainSupercampaign();
            supercampaignInstance.transform.SetParent(supercampaignContainer.transform, false);
            supercampaignInstance.transform.SetSiblingIndex(0);
        }
        else
        {
            Debug.LogError("Supercampaign not found: " + selectedSuperCampaignName);
        }
    }
}
