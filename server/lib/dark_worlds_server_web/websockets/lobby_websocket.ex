defmodule DarkWorldsServerWeb.LobbyWebsocket do
  require Logger
  alias DarkWorldsServer.Communication
  alias DarkWorldsServer.Matchmaking

  @behaviour :cowboy_websocket

  @impl true
  def init(req, _opts) do
    lobby_id = :cowboy_req.binding(:lobby_id, req)
    {:cowboy_websocket, req, %{lobby_id: lobby_id}}
  end

  @impl true
  def websocket_init(%{lobby_id: :undefined}) do
    {:stop, %{}}
  end

  def websocket_init(%{lobby_id: lobby_id}) do
    Phoenix.PubSub.subscribe(DarkWorldsServer.PubSub, Matchmaking.session_topic(lobby_id))

    matchmaking_session_pid = Communication.external_id_to_pid(lobby_id)
    players = Matchmaking.list_players(matchmaking_session_pid)
    player_id = Enum.count(players) + 1
    Matchmaking.add_player(player_id, matchmaking_session_pid)

    {:reply, {:binary, Communication.lobby_connected!(lobby_id, player_id)},
     %{lobby_pid: matchmaking_session_pid, player_id: player_id}}
  end

  @impl true
  def websocket_handle({:binary, message}, state) do
    case Communication.lobby_decode(message) do
      {:ok, %{type: :START_GAME} = message} ->
        Matchmaking.start_game(message[:game_config], state[:lobby_pid])
        {:ok, state}

      {:error, msg} ->
        Logger.error("Received frame with an invalid message: #{msg}")
        {:ok, state}
    end
  end

  @impl true
  def websocket_info({:player_added, player_id, players}, state) do
    {:reply, {:binary, Communication.lobby_player_added!(player_id, players)}, state}
  end

  def websocket_info({:player_removed, player_id, players}, state) do
    {:reply, {:binary, Communication.lobby_player_removed!(player_id, players)}, state}
  end

  def websocket_info({:game_started, game_pid}, state) do
    new_state = Map.put(state, :game_started, true)
    {:reply, {:binary, Communication.lobby_game_started!(game_pid)}, new_state}
  end

  @impl true
  def terminate(_reason, _partialreq, %{lobby_pid: lobby_pid, player_id: player_id} = state) do
    unless state[:game_started] do
      Matchmaking.remove_player(player_id, lobby_pid)
    end

    :ok
  end
end
