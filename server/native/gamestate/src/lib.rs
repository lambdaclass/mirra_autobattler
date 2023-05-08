mod board;
mod game;
mod player;
mod time_utils;

use std::collections::HashMap;

use game::GameState;
use rustler::{Env, Term};

use crate::{board::GridResource, board::Tile, game::Direction, player::Position};

#[rustler::nif(schedule = "DirtyCpu")]
fn new_game(
    number_of_players: u64,
    board_width: usize,
    board_height: usize,
    build_walls: bool,
) -> GameState {
    GameState::new(number_of_players, board_width, board_height, build_walls)
}

#[rustler::nif(schedule = "DirtyCpu")]
fn move_player(game: GameState, player_id: u64, direction: Direction) -> GameState {
    let mut game_2 = game;
    game_2.move_player(player_id, direction);
    game_2
}

#[rustler::nif(schedule = "DirtyCpu")]
fn get_grid(game: GameState) -> Vec<Vec<Tile>> {
    let grid = game.board.grid.resource.lock().unwrap();
    grid.clone()
}

#[rustler::nif(schedule = "DirtyCpu")]
fn get_non_empty(game: GameState) -> HashMap<(usize, usize), Tile> {
    let mut result: HashMap<(usize, usize), Tile> = HashMap::new();
    let grid = game.board.grid.resource.lock().unwrap();
    for (x, row) in grid.iter().enumerate() {
        for (y, e) in row.iter().enumerate() {
            match e {
                Tile::Empty => continue,
                _ => result.insert((x, y), (*e).clone()),
            };
        }
    }
    result
}

#[rustler::nif(schedule = "DirtyCpu")]
fn attack_player(
    game: GameState,
    attacking_player_id: u64,
    attack_direction: Direction,
) -> GameState {
    let mut game_2 = game;
    game_2.attack_player(attacking_player_id, attack_direction);
    game_2
}

#[rustler::nif(schedule = "DirtyCpu")]
fn attack_aoe(game: GameState, attacking_player_id: u64, center_of_attack: Position) -> GameState {
    let mut game_2 = game;
    game_2.attack_aoe(attacking_player_id, &center_of_attack);
    game_2
}

fn load(env: Env, _: Term) -> bool {
    rustler::resource!(GridResource, env);
    true
}

rustler::init!(
    "Elixir.DarkWorldsServer.Engine.Game",
    [
        new_game,
        move_player,
        get_grid,
        get_non_empty,
        attack_player,
        attack_aoe
    ],
    load = load
);
