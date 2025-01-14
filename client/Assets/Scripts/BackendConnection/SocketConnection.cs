using Google.Protobuf;
using NativeWebSocket;
using Protobuf.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SocketConnection : MonoBehaviour
{
    WebSocket ws;

    public static SocketConnection Instance;

    public bool connected = false;

    private WebSocketMessageEventHandler currentMessageHandler;

    async void Start()
    {
        Init();
    }

    public void Init()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!connected)
        {
            connected = true;
            ConnectToSession();
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
        {
            ws.DispatchMessageQueue();
        }
#endif
    }

    private async void OnApplicationQuit()
    {
        await this.CloseConnection();
    }

    private void ConnectToSession()
    {
        string url = $"{ServerSelect.Domain}/2";
        ws = new WebSocket(url);
        ws.OnMessage += OnWebSocketMessage;
        ws.OnClose += OnWebsocketClose;
        ws.OnOpen += () =>
        {
            GetUserAndContinue();
        };
        ws.OnError += (e) =>
        {
            Debug.LogError("Received error: " + e);
        };
        ws.Connect();
    }

    private void OnWebsocketClose(WebSocketCloseCode closeCode)
    {
        if (closeCode != WebSocketCloseCode.Normal)
        {
            // Errors.Instance.HandleNetworkError(connectionTitle, connectionDescription);
            Debug.LogError("Your connection to the server has been lost.");
        }
        else
        {
            Debug.Log("ServerConnection websocket closed normally.");
        }
    }

    private void SendWebSocketMessage<T>(IMessage<T> message)
        where T : IMessage<T>
    {
        using (var stream = new MemoryStream())
        {
            message.WriteTo(stream);
            var msg = stream.ToArray();

            if (ws != null)
            {
                ws.Send(msg);
            }
        }
    }

    public async Task CloseConnection()
    {
        if (connected && this.ws != null)
        {
            await this.ws.Close();
            connected = false;
        }
    }

    private void OnWebSocketMessage(byte[] data)
    {
        try
        {
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            switch (webSocketResponse.ResponseTypeCase)
            {
                case WebSocketResponse.ResponseTypeOneofCase.Error:
                    Debug.Log(webSocketResponse.Error.Reason);
                    break;
                // Since the response of type Unit isn't used for anything there isn't a specific handler for it, it is caught here so it doesn't log any confusing messages
                case WebSocketResponse.ResponseTypeOneofCase.Unit:
                    break;
                default:
                    Debug.Log("Request case not handled");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("InvalidProtocolBufferException: " + e);
        }
    }

    private User CreateUserFromData(Protobuf.Messages.User user, List<Character> availableCharacters)
    {
        List<Unit> units = CreateUnitsFromData(user.Units, availableCharacters);
        List<Item> items = new List<Item>();

        foreach (var userItem in user.Items)
        {
            items.Add(CreateItemFromData(userItem));
        }

        Dictionary<string, int> currencies = new Dictionary<string, int>();

        foreach (var currency in user.Currencies)
        {
            if (GlobalUserData.Instance.AvailableCurrencies.Any(availableCurrency => availableCurrency.name == currency.Currency.Name))
            {
                currencies.Add(currency.Currency.Name, (int)currency.Amount);
            }
            else
            {
                Debug.LogError($"Currency brought from the backend not found in client: {currency.Currency.Name}");
            }
        }

        KalineTreeLevel kalineTreeLevel = new KalineTreeLevel
        {
            level = (int)user.KalineTreeLevel.Level,
            goldLevelUpCost = ((int)user.KalineTreeLevel.GoldLevelUpCost),
            fertilizerLevelUpCost = ((int)user.KalineTreeLevel.FertilizerLevelUpCost),
            afkRewardRates = user.KalineTreeLevel.AfkRewardRates.Select(afkRewardRate => CreateAfkRewardRateFromData(afkRewardRate)).ToList()
        };

        DungeonSettlementLevel dungeonSettlementLevel = new DungeonSettlementLevel
        {
            level = (int)user.DungeonSettlementLevel.Level,
            levelUpCosts = user.DungeonSettlementLevel.LevelUpCosts.Select(cost => new CurrencyCost { currency = new Currency { name = cost.Currency.Name }, amount = (int)cost.Amount }).ToList(),
            maxDungeon = (int)user.DungeonSettlementLevel.MaxDungeon,
            maxFactional = (int)user.DungeonSettlementLevel.MaxFactional,
            supplyLimit = (int)user.DungeonSettlementLevel.SupplyLimit,
            afkRewardRates = user.DungeonSettlementLevel.AfkRewardRates.Select(afkRewardRate => CreateAfkRewardRateFromData(afkRewardRate)).ToList()
        };

        return new User
        {
            id = user.Id,
            username = user.Username,
            units = units,
            items = items,
            currencies = currencies,
            level = (int)user.Level,
            experience = (int)user.Experience,
            kalineTreeLevel = kalineTreeLevel,
            dungeonSettlementLevel = dungeonSettlementLevel
        };
    }
    private Item CreateItemFromData(Protobuf.Messages.Item itemData)
    {
        return new Item
        {
            id = itemData.Id,
            level = (int)itemData.Level,
            userId = itemData.UserId,
            unitId = itemData.UnitId,
            template = GlobalUserData.Instance.AvailableItemTemplates.Find(itemtemplate => itemtemplate.name.ToLower() == itemData.Template.Name.ToLower())
        };
    }

    private AfkRewardRate CreateAfkRewardRateFromData(Protobuf.Messages.AfkRewardRate afkRewardRateData)
    {
        return new AfkRewardRate
        {
            kalineTreeLevelId = afkRewardRateData.KalineTreeLevelId,
            currency = afkRewardRateData.Currency.Name,
            daily_rate = afkRewardRateData.DailyRate
        };
    }

    private Unit CreateUnitFromData(Protobuf.Messages.Unit unitData, List<Character> availableCharacters)
    {
        if (!availableCharacters.Any(character => character.name.ToLower() == unitData.Character.Name.ToLower()))
        {
            Debug.Log($"Character not found in available characters: {unitData.Character.Name}");
            return null;
        }

        return new Unit
        {
            id = unitData.Id,
            // tier = (int)unitData.Tier,
            character = availableCharacters.Find(character => character.name.ToLower() == unitData.Character.Name.ToLower()),
            rank = (Rank)unitData.Rank,
            level = (int)unitData.Level,
            tier = (int)unitData.Tier,
            slot = (int?)unitData.Slot,
            selected = unitData.Selected
        };
    }

    private List<Unit> CreateUnitsFromData(IEnumerable<Protobuf.Messages.Unit> unitsData, List<Character> availableCharacters)
    {
        List<Unit> createdUnits = new List<Unit>();

        foreach (var unitData in unitsData)
        {
            Unit unit = CreateUnitFromData(unitData, availableCharacters);

            if (unit != null)
            {
                createdUnits.Add(unit);
            }
        }

        return createdUnits;
    }

    private List<Campaign> ParseCampaignsFromResponse(Protobuf.Messages.Campaigns campaignsData, string superCampaignName, List<Character> availableCharacters)
    {
        List<(string superCampaignName, string campaignId, string levelId)> userCampaignsProgress = GlobalUserData.Instance.User.supercampaignsProgresses;
        List<Campaign> campaigns = new List<Campaign>();
        LevelProgress.Status campaignStatus = LevelProgress.Status.Completed;

        // Filter campaigns by supercampaign name
        var superCampaignCampaigns = campaignsData.Campaigns_.Where(campaign => campaign.SuperCampaignName == superCampaignName).ToList();

        foreach (Protobuf.Messages.Campaign campaignData in superCampaignCampaigns)
        {
            LevelProgress.Status levelStatus = LevelProgress.Status.Completed;
            List<LevelData> levels = new List<LevelData>();

            foreach (Protobuf.Messages.Level level in campaignData.Levels.OrderBy(level => level.LevelNumber))
            {
                List<Unit> levelUnits = CreateUnitsFromData(level.Units, availableCharacters);

                levelStatus = userCampaignsProgress.Any(cp => cp.campaignId == campaignData.Id && cp.levelId == level.Id) ? LevelProgress.Status.Unlocked : levelStatus;

                levels.Add(new LevelData
                {
                    id = level.Id,
                    levelNumber = (int)level.LevelNumber,
                    campaignId = level.CampaignId,
                    units = levelUnits,
                    rewards = GetLevelCurrencyRewards(level),
                    status = levelStatus
                });


                if (levelStatus == LevelProgress.Status.Unlocked)
                {
                    levelStatus = LevelProgress.Status.Locked;
                }
            }

            campaignStatus = userCampaignsProgress.Any(cp => cp.campaignId == campaignData.Id) ? LevelProgress.Status.Unlocked : campaignStatus;

            campaigns.Add(new Campaign
            {
                campaignId = campaignData.Id,
                campaignNumber = (int)campaignData.CampaignNumber,
                status = campaignStatus,
                levels = levels
            });

            if (campaignStatus == LevelProgress.Status.Unlocked)
            {
                campaignStatus = LevelProgress.Status.Locked;
            }
        }

        return campaigns;
    }

    // Better name for this method?
    // This should be refactored, assigning player prefs should not be handled here
    public void GetUserAndContinue()
    {
        string userId = PlayerPrefs.GetString($"userId_{ServerSelect.Name}");
        if (String.IsNullOrEmpty(userId))
        {
            string userName = $"testUser-{Guid.NewGuid()}";
            Debug.Log($"No user in player prefs, creating user with username: \"{userName}\"");
            CreateUser(userName, (user) =>
            {
                GetSupercampaignProgresses(user.id, (progresses) =>
                {
                    user.supercampaignsProgresses = progresses;
                });
                PlayerPrefs.SetString($"userId_{ServerSelect.Name}", user.id);
                GlobalUserData.Instance.User = user;
                Debug.Log("User created correctly");
            });
        }
        else
        {
            Debug.Log($"Found userid: \"{userId}\" in playerprefs, getting the user");
            GetUser(userId, (user) =>
            {
                GetSupercampaignProgresses(user.id, (progresses) =>
                {
                    user.supercampaignsProgresses = progresses;
                });
                PlayerPrefs.SetString($"userId_{ServerSelect.Name}", user.id);
                GlobalUserData.Instance.User = user;
            });
        }
    }

    public void GetUser(string userId, Action<User> onGetUserDataReceived)
    {
        try
        {
            GetUser getUserRequest = new GetUser
            {
                UserId = userId
            };
            WebSocketRequest request = new WebSocketRequest
            {
                GetUser = getUserRequest
            };
            currentMessageHandler = (data) => AwaitGetUserResponse(data, onGetUserDataReceived);
            ws.OnMessage += currentMessageHandler;
            ws.OnMessage -= OnWebSocketMessage;
            SendWebSocketMessage(request);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public void GetUserByUsername(string username, Action<User> onGetUserDataReceived)
    {
        GetUserByUsername getUserByUsernameRequest = new GetUserByUsername
        {
            Username = username
        };
        WebSocketRequest request = new WebSocketRequest
        {
            GetUserByUsername = getUserByUsernameRequest
        };
        currentMessageHandler = (data) => AwaitGetUserResponse(data, onGetUserDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitGetUserResponse(byte[] data, Action<User> onGetUserDataReceived)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.User)
            {
                User user = CreateUserFromData(webSocketResponse.User, GlobalUserData.Instance.AvailableCharacters);
                onGetUserDataReceived?.Invoke(user);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                switch (webSocketResponse.Error.Reason)
                {
                    case "not_found":
                        string userName = $"testUser-{Guid.NewGuid()}";
                        Debug.Log($"User not found, trying to create new user with username: \"{userName}\"");
                        CreateUser(userName, (user) =>
                        {
                            onGetUserDataReceived?.Invoke(user);
                        });
                        break;
                    case "username_taken":
                        Debug.LogError("Tried to create user, username was taken");
                        break;
                    default:
                        Debug.LogError(webSocketResponse.Error.Reason);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("InvalidProtocolBufferException: " + e);
        }
    }

    public void CreateUser(string username, Action<User> onGetUserDataReceived)
    {
        CreateUser createUserRequest = new CreateUser
        {
            Username = username
        };
        WebSocketRequest request = new WebSocketRequest
        {
            CreateUser = createUserRequest
        };
        currentMessageHandler = (data) => AwaitGetUserResponse(data, onGetUserDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void GetSupercampaignProgresses(string userId, Action<List<(string, string, string)>> onSupercampaignProgressesReceived)
    {
        GetUserSuperCampaignProgresses getSupercampaignsProgressesRequest = new GetUserSuperCampaignProgresses
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            GetUserSuperCampaignProgresses = getSupercampaignsProgressesRequest
        };
        currentMessageHandler = (data) => AwaitSupercampaignsProgressesResponse(data, onSupercampaignProgressesReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitSupercampaignsProgressesResponse(byte[] data, Action<List<(string, string, string)>> onSupercampaignProgressReceived)
    {
        try
        {
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.SuperCampaignProgresses)
            {
                List<(string, string, string)> campaignProgresses = webSocketResponse.SuperCampaignProgresses.SuperCampaignProgresses_.Select(cp => (cp.SuperCampaignName, cp.CampaignId, cp.LevelId)).ToList();
                onSupercampaignProgressReceived?.Invoke(campaignProgresses);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void GetCampaigns(string userId, string superCampaignName, Action<List<Campaign>> onCampaignDataReceived)
    {
        GetCampaigns getCampaignsRequest = new GetCampaigns
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            GetCampaigns = getCampaignsRequest
        };
        currentMessageHandler = (data) => AwaitGetCampaignsResponse(data, onCampaignDataReceived, superCampaignName);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitGetCampaignsResponse(byte[] data, Action<List<Campaign>> onCampaignDataReceived, string superCampaignName)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Campaigns)
            {
                List<Character> availableCharacters = GlobalUserData.Instance.AvailableCharacters;
                List<Campaign> campaigns = ParseCampaignsFromResponse(webSocketResponse.Campaigns, superCampaignName, availableCharacters);
                onCampaignDataReceived?.Invoke(campaigns);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void SelectUnit(string unitId, string userId, int slot)
    {
        SelectUnit selectUnitRequest = new SelectUnit
        {
            UserId = userId,
            UnitId = unitId,
            Slot = (uint)slot
        };
        WebSocketRequest request = new WebSocketRequest
        {
            SelectUnit = selectUnitRequest
        };
        SendWebSocketMessage(request);
    }

    public void UnselectUnit(string unitId, string userId)
    {
        UnselectUnit unselectUnitRequest = new UnselectUnit
        {
            UserId = userId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            UnselectUnit = unselectUnitRequest
        };
        SendWebSocketMessage(request);
    }

    private void AwaitBattleResponse(byte[] data, Action<BattleResult> onBattleResultReceived)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.BattleResult)
            {
                onBattleResultReceived?.Invoke(webSocketResponse.BattleResult);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public IEnumerator Battle(string userId, string levelId, Action<BattleResult> onBattleResultReceived)
    {
        FightLevel fightLevelRequest = new FightLevel
        {
            UserId = userId,
            LevelId = levelId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            FightLevel = fightLevelRequest
        };
        currentMessageHandler = (data) => AwaitBattleResponse(data, onBattleResultReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
        yield return null;
    }

    public void EquipItem(string userId, string itemId, string unitId, Action<Item> onItemDataReceived)
    {
        EquipItem equipItemRequest = new EquipItem
        {
            UserId = userId,
            ItemId = itemId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            EquipItem = equipItemRequest
        };
        currentMessageHandler = (data) => AwaitItemResponse(data, onItemDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void UnequipItem(string userId, string itemId, Action<Item> onItemDataReceived)
    {
        UnequipItem unequipItemRequest = new UnequipItem
        {
            UserId = userId,
            ItemId = itemId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            UnequipItem = unequipItemRequest
        };
        currentMessageHandler = (data) => AwaitItemResponse(data, onItemDataReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void LevelUpItem(string userId, string itemId, Action<Item> onItemDataReceived, Action<string> onError)
    {
        LevelUpItem levelUpItemRequest = new LevelUpItem
        {
            UserId = userId,
            ItemId = itemId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            LevelUpItem = levelUpItemRequest
        };
        currentMessageHandler = (data) => AwaitItemResponse(data, onItemDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitItemResponse(byte[] data, Action<Item> onItemDataReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Item)
            {
                Item item = CreateItemFromData(webSocketResponse.Item);
                onItemDataReceived?.Invoke(item);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void LevelUpUnit(string userId, string unitId, Action<Protobuf.Messages.UnitAndCurrencies> onUnitAndCurrenciesDataReceived, Action<string> onError)
    {
        LevelUpUnit levelUpUnitRequest = new LevelUpUnit
        {
            UserId = userId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            LevelUpUnit = levelUpUnitRequest
        };
        currentMessageHandler = (data) => AwaitUnitAndCurrenciesReponse(data, onUnitAndCurrenciesDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void TierUpUnit(string userId, string unitId, Action<Protobuf.Messages.UnitAndCurrencies> onUnitAndCurrenciesDataReceived, Action<string> onError)
    {
        TierUpUnit tierUpUnitRequest = new TierUpUnit
        {
            UserId = userId,
            UnitId = unitId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            TierUpUnit = tierUpUnitRequest
        };
        currentMessageHandler = (data) => AwaitUnitAndCurrenciesReponse(data, onUnitAndCurrenciesDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }


    private void AwaitUnitAndCurrenciesReponse(byte[] data, Action<Protobuf.Messages.UnitAndCurrencies> onUnitAndCurrenciesDataReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.UnitAndCurrencies)
            {
                onUnitAndCurrenciesDataReceived?.Invoke(webSocketResponse.UnitAndCurrencies);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void GetBoxes(Action<List<Box>> onBoxesDataReceived, Action<string> onError = null)
    {
        GetBoxes getBoxesRequest = new GetBoxes();
        WebSocketRequest request = new WebSocketRequest
        {
            GetBoxes = getBoxesRequest
        };
        currentMessageHandler = (data) => AwaitBoxesReponse(data, onBoxesDataReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitBoxesReponse(byte[] data, Action<List<Box>> onBoxesDataReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Boxes)
            {
                List<Box> boxes = ParseBoxesFromResponse(webSocketResponse.Boxes);
                onBoxesDataReceived?.Invoke(boxes);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private List<Box> ParseBoxesFromResponse(Boxes boxesMessage)
    {
        return boxesMessage.Boxes_.Select(box =>
        {
            return new Box
            {
                id = box.Id,
                name = box.Name,
                description = box.Description,
                factions = box.Factions.ToList(),
                rankWeights = box.RankWeights.ToDictionary(rankWeight => rankWeight.Rank, rankWeight => rankWeight.Weight),
                costs = box.Cost.ToDictionary(cost => cost.Currency.Name, cost => cost.Amount)
            };
        }).ToList();
    }

    public void Summon(string userId, string boxId, Action<User, Unit> onSuccess, Action<string> onError = null)
    {
        Summon summonRequest = new Summon
        {
            UserId = userId,
            BoxId = boxId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            Summon = summonRequest
        };
        currentMessageHandler = (data) => AwaitSummonResponse(data, onSuccess, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitSummonResponse(byte[] data, Action<User, Unit> onSuccess, Action<string> onError)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.UserAndUnit)
            {
                User user = CreateUserFromData(webSocketResponse.UserAndUnit.User, GlobalUserData.Instance.AvailableCharacters);
                Unit unit = CreateUnitFromData(webSocketResponse.UserAndUnit.Unit, GlobalUserData.Instance.AvailableCharacters);
                onSuccess?.Invoke(user, unit);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void FuseUnits(string userId, string unitId, string[] consumedUnitsIds, Action<Unit> onSuccess, Action<string> onError = null)
    {
        FuseUnit fuseRequest = new FuseUnit
        {
            UserId = userId,
            UnitId = unitId,
        };
        // Why on earth repeated fields don't have a setter but you can still add values to them?
        foreach (string consumedUnitId in consumedUnitsIds)
        {
            fuseRequest.ConsumedUnitsIds.Add(consumedUnitId);
        }
        WebSocketRequest request = new WebSocketRequest
        {
            FuseUnit = fuseRequest
        };
        currentMessageHandler = (data) => AwaitFuseUnitsResponse(data, onSuccess, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitFuseUnitsResponse(byte[] data, Action<Unit> onSuccess, Action<string> onError)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Unit)
            {
                Unit unit = CreateUnitFromData(webSocketResponse.Unit, GlobalUserData.Instance.AvailableCharacters);
                onSuccess?.Invoke(unit);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private Dictionary<string, int> GetLevelCurrencyRewards(Level level)
    {
        Dictionary<string, int> rewards = level.CurrencyRewards.ToDictionary(currencyReward => currencyReward.Currency.Name, currencyReward => (int)currencyReward.Amount);
        rewards.Add("Experience", (int)level.ExperienceReward);
        return rewards;
    }

    public void GetKalineAfkRewards(string userId, Action<List<AfkReward>> onAfkRewardsReceived)
    {
        GetKalineAfkRewards getKalineAfkRewardsRequest = new GetKalineAfkRewards
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            GetKalineAfkRewards = getKalineAfkRewardsRequest
        };
        currentMessageHandler = (data) => AwaitGetAfkRewardsResponse(data, onAfkRewardsReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void GetDungeonAfkRewards(string userId, Action<List<AfkReward>> onAfkRewardsReceived)
    {
        GetDungeonAfkRewards getDungeonAfkRewardsRequest = new GetDungeonAfkRewards
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            GetDungeonAfkRewards = getDungeonAfkRewardsRequest
        };
        currentMessageHandler = (data) => AwaitGetAfkRewardsResponse(data, onAfkRewardsReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }


    private void AwaitGetAfkRewardsResponse(byte[] data, Action<List<AfkReward>> onAfkRewardsReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.AfkRewards)
            {
                List<AfkReward> afkRewards = webSocketResponse.AfkRewards.AfkRewards_.Select(afkReward => new AfkReward
                {
                    currency = afkReward.Currency.Name,
                    amount = (int)afkReward.Amount
                }).ToList();
                onAfkRewardsReceived?.Invoke(afkRewards);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void ClaimKalineAfkRewards(string userId, Action<User> onAfkRewardsReceived)
    {
        ClaimKalineAfkRewards claimKalineAfkRewardsRequest = new ClaimKalineAfkRewards
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            ClaimKalineAfkRewards = claimKalineAfkRewardsRequest
        };
        currentMessageHandler = (data) => AwaitClaimAfkRewardsResponse(data, onAfkRewardsReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    public void ClaimDungeonAfkRewards(string userId, Action<User> onAfkRewardsReceived)
    {
        ClaimDungeonAfkRewards claimDungeonAfkRewardsRequest = new ClaimDungeonAfkRewards
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            ClaimDungeonAfkRewards = claimDungeonAfkRewardsRequest
        };
        currentMessageHandler = (data) => AwaitClaimAfkRewardsResponse(data, onAfkRewardsReceived);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitClaimAfkRewardsResponse(byte[] data, Action<User> onAfkRewardsReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.User)
            {
                User user = CreateUserFromData(webSocketResponse.User, GlobalUserData.Instance.AvailableCharacters);
                onAfkRewardsReceived?.Invoke(user);
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void LevelUpKalineTree(string userId, Action<User> onLeveledUpUserReceived, Action<string> onError = null)
    {
        LevelUpKalineTree levelUpKalineTreeRequest = new LevelUpKalineTree
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            LevelUpKalineTree = levelUpKalineTreeRequest
        };
        currentMessageHandler = (data) => AwaitLevelUpKalineTreeResponse(data, onLeveledUpUserReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitLevelUpKalineTreeResponse(byte[] data, Action<User> onLeveledUpUserReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.User)
            {
                User user = CreateUserFromData(webSocketResponse.User, GlobalUserData.Instance.AvailableCharacters);
                onLeveledUpUserReceived?.Invoke(user);
                GlobalUserData.Instance.User.kalineTreeLevel = user.kalineTreeLevel;
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void LevelUpDungeonSettlement(string userId, Action<User> onLeveledUpUserReceived, Action<string> onError = null)
    {
        LevelUpDungeonSettlement levelUpDungeonSettlement = new LevelUpDungeonSettlement
        {
            UserId = userId
        };
        WebSocketRequest request = new WebSocketRequest
        {
            LevelUpDungeonSettlement = levelUpDungeonSettlement
        };
        currentMessageHandler = (data) => AwaitLevelUpDungeonSettlementResponse(data, onLeveledUpUserReceived, onError);
        ws.OnMessage += currentMessageHandler;
        ws.OnMessage -= OnWebSocketMessage;
        SendWebSocketMessage(request);
    }

    private void AwaitLevelUpDungeonSettlementResponse(byte[] data, Action<User> onLeveledUpUserReceived, Action<string> onError = null)
    {
        try
        {
            ws.OnMessage -= currentMessageHandler;
            ws.OnMessage += OnWebSocketMessage;
            WebSocketResponse webSocketResponse = WebSocketResponse.Parser.ParseFrom(data);
            if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.User)
            {
                User user = CreateUserFromData(webSocketResponse.User, GlobalUserData.Instance.AvailableCharacters);
                onLeveledUpUserReceived?.Invoke(user);
                GlobalUserData.Instance.User.dungeonSettlementLevel = user.dungeonSettlementLevel;
            }
            else if (webSocketResponse.ResponseTypeCase == WebSocketResponse.ResponseTypeOneofCase.Error)
            {
                onError?.Invoke(webSocketResponse.Error.Reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

}
