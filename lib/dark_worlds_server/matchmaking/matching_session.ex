defmodule DarkWorldsServer.Matchmaking.MatchingSession do
  alias DarkWorldsServer.Matchmaking
  use GenServer, restart: :transient

  # 2 minutes
  @timeout_ms 2 * 60 * 1000

  #######
  # API #
  #######
  def start_link(_args) do
    GenServer.start_link(__MODULE__, [])
  end

  def add_player(player_id, session_pid) do
    GenServer.call(session_pid, {:add_player, player_id})
  end

  def remove_player(player_id, session_pid) do
    GenServer.call(session_pid, {:remove_player, player_id})
  end

  #######################
  # GenServer callbacks #
  #######################
  @impl GenServer
  def init(_args) do
    Process.send_after(self(), :check_timeout, @timeout_ms * 2)
    session_id = :erlang.term_to_binary(self()) |> Base.encode64()
    topic = Matchmaking.session_topic(session_id)
    {:ok, %{players: [], session_id: session_id, topic: topic}}
  end

  @impl GenServer
  def handle_call({:add_player, player_id}, _from, state) do
    players = state[:players]

    case Enum.member?(players, player_id) do
      true ->
        {:reply, :ok, state}

      false ->
        send(self(), :player_added)
        {:reply, :ok, %{state | :players => [player_id | players]}}
    end
  end

  def handle_call({:remove_player, player_id}, _from, state) do
    players = state[:players]

    case List.delete(players, player_id) do
      ^players ->
        {:reply, :ok, state}

      remaining_players ->
        send(self(), :player_removed)
        {:reply, :ok, %{state | :players => remaining_players}}
    end
  end

  @impl GenServer
  def handle_info(:player_added, state) do
    Phoenix.PubSub.broadcast!(
      DarkWorldsServer.PubSub,
      state[:topic],
      {:player_added, length(state[:players])}
    )

    {:noreply, state}
  end

  def handle_info(:player_removed, state) do
    Phoenix.PubSub.broadcast!(
      DarkWorldsServer.PubSub,
      state[:topic],
      {:player_removed, length(state[:players])}
    )

    {:noreply, state}
  end

  def handle_info(:pong, state) do
    {:noreply, state}
  end

  def handle_info(:check_timeout, state) do
    Phoenix.PubSub.broadcast!(DarkWorldsServer.PubSub, state[:topic], {:ping, self()})
    Process.send_after(self(), :set_timeout, @timeout_ms * 2)
    {:noreply, state, @timeout_ms}
  end
end
