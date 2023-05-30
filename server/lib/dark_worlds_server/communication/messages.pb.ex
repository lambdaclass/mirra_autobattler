defmodule DarkWorldsServer.Communication.Proto.GameEventType do
  @moduledoc false

  use Protobuf, enum: true, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :STATE_UPDATE, 0
  field :PING_UPDATE, 1
end

defmodule DarkWorldsServer.Communication.Proto.Status do
  @moduledoc false

  use Protobuf, enum: true, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :ALIVE, 0
  field :DEAD, 1
end

defmodule DarkWorldsServer.Communication.Proto.Action do
  @moduledoc false

  use Protobuf, enum: true, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :ACTION_UNSPECIFIED, 0
  field :MOVE, 1
  field :ATTACK, 2
  field :ATTACK_AOE, 5
  field :MOVE_WITH_JOYSTICK, 6
end

defmodule DarkWorldsServer.Communication.Proto.Direction do
  @moduledoc false

  use Protobuf, enum: true, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :DIRECTION_UNSPECIFIED, 0
  field :UP, 1
  field :DOWN, 2
  field :LEFT, 3
  field :RIGHT, 4
end

defmodule DarkWorldsServer.Communication.Proto.PlayerAction do
  @moduledoc false

  use Protobuf, enum: true, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :NOTHING, 0
  field :ATTACKING, 1
  field :ATTACKING_AOE, 2
end

defmodule DarkWorldsServer.Communication.Proto.LobbyEventType do
  @moduledoc false

  use Protobuf, enum: true, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :TYPE_UNSPECIFIED, 0
  field :CONNECTED, 1
  field :PLAYER_ADDED, 2
  field :GAME_STARTED, 3
  field :PLAYER_COUNT, 4
  field :START_GAME, 5
  field :PLAYER_REMOVED, 6
end

defmodule DarkWorldsServer.Communication.Proto.GameEvent do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :type, 1, type: DarkWorldsServer.Communication.Proto.GameEventType, enum: true
  field :players, 2, repeated: true, type: DarkWorldsServer.Communication.Proto.Player
  field :latency, 3, type: :uint64

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.Player do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :id, 1, type: :uint64
  field :health, 2, type: :sint64
  field :position, 3, type: DarkWorldsServer.Communication.Proto.Position
  field :last_melee_attack, 4, type: :uint64, json_name: "lastMeleeAttack"
  field :status, 5, type: DarkWorldsServer.Communication.Proto.Status, enum: true
  field :action, 6, type: DarkWorldsServer.Communication.Proto.PlayerAction, enum: true

  field :aoe_position, 7,
    type: DarkWorldsServer.Communication.Proto.Position,
    json_name: "aoePosition"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.Position do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :x, 1, type: :uint64
  field :y, 2, type: :uint64

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.RelativePosition do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :x, 1, type: :int64
  field :y, 2, type: :int64

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.ClientAction do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :action, 1, type: DarkWorldsServer.Communication.Proto.Action, enum: true
  field :direction, 2, type: DarkWorldsServer.Communication.Proto.Direction, enum: true
  field :position, 3, type: DarkWorldsServer.Communication.Proto.RelativePosition

  field :move_delta, 4,
    type: DarkWorldsServer.Communication.Proto.JoystickValues,
    json_name: "moveDelta"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.JoystickValues do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :x, 1, type: :float
  field :y, 2, type: :float

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.LobbyEvent do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :type, 1, type: DarkWorldsServer.Communication.Proto.LobbyEventType, enum: true
  field :lobby_id, 2, type: :string, json_name: "lobbyId"
  field :player_id, 3, type: :uint64, json_name: "playerId"
  field :added_player_id, 4, type: :uint64, json_name: "addedPlayerId"
  field :game_id, 5, type: :string, json_name: "gameId"
  field :player_count, 6, type: :uint64, json_name: "playerCount"
  field :players, 7, repeated: true, type: :uint64
  field :removed_player_id, 8, type: :uint64, json_name: "removedPlayerId"

  field :game_config, 9,
    type: DarkWorldsServer.Communication.Proto.GameConfig,
    json_name: "gameConfig"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.BoardSize do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :width, 1, type: :uint64
  field :height, 2, type: :uint64

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameConfig do
  @moduledoc false

  use Protobuf, protoc_gen_elixir_version: "0.12.0", syntax: :proto3

  field :boardSize, 1, type: DarkWorldsServer.Communication.Proto.BoardSize
  field :serverTickRate, 2, type: :uint64
  field :gameTimeOut, 3, type: :uint64
  field :characterSpeed, 4, type: :uint64

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end