module aptosvictors::gameplaymanager {
    use std::signer;
    use aptos_framework::timestamp;
    use aptos_framework::event;

    // Struct to hold game state
    struct GameState has key {
        score: u64,
        star_score: u64,
        best_score: u64,
        last_update: u64,
        game_active: bool,
        total_stars: u64,
}

    // Events
    #[event]
    struct ScoreUpdatedEvent has drop, store {
        player: address,
        new_score: u64,
    }
    #[event]
    struct StarCollectedEvent has drop, store {
        player: address,
        new_star_count: u64,
    }

    #[event]
    struct PurchaseMadeEvent has drop, store {
        player: address,
        item_cost: u64,
        new_star_count: u64,
    }

    // Initialize a new game state for a player
    public entry fun initialize_game(account: &signer) {
        let player_addr = signer::address_of(account);
        assert!(!exists<GameState>(player_addr), 1); // Error code 1: Game already initialized

        move_to(account, GameState {
            score: 0,
            star_score: 0,
            best_score: 0,
            last_update: timestamp::now_seconds(),
            game_active: false,
            total_stars: 0,
        });
    }

    // Start a new game session
    public entry fun start_game(account: &signer) acquires GameState {
        let player_addr = signer::address_of(account);
        assert!(exists<GameState>(player_addr), 2); // Error code 2: Game not initialized

        let game_state = borrow_global_mut<GameState>(player_addr);
        game_state.game_active = true;
        game_state.score = 0;
        game_state.star_score = 0;
        game_state.last_update = timestamp::now_seconds();
    
    }

    // Update the player's score
    public entry fun update_score(account: &signer, new_score: u64) acquires GameState {
        let player_addr = signer::address_of(account);
        let game_state = borrow_global_mut<GameState>(player_addr);
        
        assert!(game_state.game_active, 3); // Error code 3: Game not active
        
        game_state.score = new_score;
        if (new_score > game_state.best_score) {
            game_state.best_score = new_score;
        };
        
        // Emit score updated event
        event::emit(ScoreUpdatedEvent {
            player: player_addr,
            new_score,
        });
    }

    // Collect a star
    public entry fun collect_star(account: &signer) acquires GameState {
        let player_addr = signer::address_of(account);
        let game_state = borrow_global_mut<GameState>(player_addr);
        
        assert!(game_state.game_active, 3); // Error code 3: Game not active
        
        game_state.star_score = game_state.star_score + 1;
        // Emit star collected event
        event::emit(StarCollectedEvent {
            player: player_addr,
            new_star_count: game_state.star_score,
        });
    }

    // End the game session
    public entry fun end_game(account: &signer) acquires GameState {
        let player_addr = signer::address_of(account);
        let game_state = borrow_global_mut<GameState>(player_addr);
        game_state.total_stars = game_state.total_stars + game_state.star_score;
        game_state.game_active = false;
    }

     public entry fun make_purchase(account: &signer, item_cost: u64) acquires GameState {
        let player_addr = signer::address_of(account);
        let game_state = borrow_global_mut<GameState>(player_addr);
        
        assert!(game_state.total_stars >= item_cost, 4); // Error code 4: Insufficient stars
        
        game_state.total_stars = game_state.total_stars - item_cost;
        
        // Emit purchase made event
        event::emit(PurchaseMadeEvent {
            player: player_addr,
            item_cost,
            new_star_count: game_state.total_stars,
        });
    }

    // Get the player's current score (view function)
    #[view]
    public fun get_score(player: address): u64 acquires GameState {
        borrow_global<GameState>(player).score
    }

    // Get the player's star score (view function)
    #[view]
    public fun get_star_score(player: address): u64 acquires GameState {
        borrow_global<GameState>(player).star_score
    }

    // Get the player's best score (view function)
    #[view]
    public fun get_best_score(player: address): u64 acquires GameState {
        borrow_global<GameState>(player).best_score
    }
}
