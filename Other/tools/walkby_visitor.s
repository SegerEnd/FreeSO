; Visitor init — mirrors npc_fso_witch BHAV 4096:
; call PersonGlobals "init NPC" (0x206A) which wraps Init Person and restores
; PersonType=2 (Service NPC). Avoids the npc_service_semiglobal wrapper.
BHAV [4097] "visitor init" 0x8003 args=0 locals=0
  call_semiglobal 0x206A args=(0,0,0,0)                 -> return_true / return_true


; Visitor "Walk Off Lot" interaction (registered in TTAB index 0).
; Pushed onto the visitor by the controller after placement; runs via PersonGlobals
; "person main" (8193). Uses global 0x0175 "find random portal" instead of hand-rolled
; portal lookup so the same code works on any lot regardless of sidewalk GUID layout.
BHAV [4098] "visitor walkby" 0x8003 args=0 locals=0
find:
  call_global 0x0175 args=(0,0,0,0)                     -> check / die  ; Temps[0] := random portal
check:
  Temps[0] == 0                                         -> die / grab  ; defensive: no portal -> remove without routing (avoids NRE in goto_routing_slot)
grab:
  StackObjID[0] := Temps[0]                             -> route / die
route:
  goto_routing_slot data=0 type=1                       -> die / die
die:
  remove_object target=0                                -> return_true / return_true


; Check tree for "Walk Off Lot": always returns false so the interaction never shows
; in the pie menu. push_interaction sets FSOSkipPermissions, which makes VMThread skip
; the check on queued runs — so the controller's push still executes the walkby.
BHAV [4099] "walkby pie hide" 0x8003 args=0 locals=0
  StackObjID[0] := 0                                    -> return_false / return_false


; Shared helper for pie-menu interactions: route the caller next to the visitor and
; play a generic friendly social animation. We can't use source=Object here because
; our IFF has no local STR# 128 animation table — the lookup would fall back to
; STR# 129 which we also lack. source=Misc (3) resolves via global.iff STR# 156
; (the canonical "Misc" animation pool), so we get a real animation regardless of
; what our local IFF declares. id=394 = "a2o-soc-nice-compliment-stand".
BHAV [4100] "Helper - Approach Visitor" 0x8003 args=0 locals=0
approach:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt   -> play_anim / return_false
play_anim:
  animate raw=[8A,01,00,00,03,00,00,00]                          -> return_true / play_anim  ; id=394 source=Misc (compliment-stand from global.iff STR# 156)


; Pie-menu interactions. Each is a thin wrapper around the approach helper so adding
; more is just one TTAB row + one BHAV stub. Visually similar today (chat animation);
; differentiate later by swapping animation id or adding a dialog string.
BHAV [4101] "Interaction - Wave" 0x8003 args=0 locals=0
  call_private 0x1004 args=(0,0,0,0)                    -> return_true / return_false

BHAV [4102] "Interaction - Greet" 0x8003 args=0 locals=0
  call_private 0x1004 args=(0,0,0,0)                    -> return_true / return_false

BHAV [4103] "Interaction - Compliment" 0x8003 args=0 locals=0
  call_private 0x1004 args=(0,0,0,0)                    -> return_true / return_false


; User-facing "Say Goodbye": immediate walkby (no delay). Must run in the visitor's
; thread, not the caller's — otherwise remove_object in 4098 would remove the player.
; So we push the hidden "Immediate Walkby" interaction (TTAB idx 5 -> BHAV 4098) onto
; the visitor at Maximum priority. The autonomous delayed walkby is preempted.
BHAV [4104] "Interaction - Say Goodbye" 0x8003 args=0 locals=1
  Local[0] := StackObjID[0]                             -> push / return_false  ; capture visitor id
push:
  push_interaction raw=[05,00,01,02,00,00,00,00]        -> return_true / return_false  ; idx=5 obj=Local[0] priority=Maximum flags=ObjInLocal


; Delayed walkby: hangs around ~60s of real time (1800 ticks at 30 ticks/sec) so
; the visitor isn't just instantly leaving, then runs the walkby. Pushed by the
; controller right after placement. A user-clicked "Say Goodbye" preempts this via
; higher priority (Max vs the controller's Autonomous-priority push).
BHAV [4105] "Delayed Walkby" 0x8003 args=0 locals=0
  call_global 0x0118 args=(1800,0,0,0)                  -> walkby / walkby  ; "Wait for Notify" / sleep
walkby:
  call_private 0x1002 args=(0,0,0,0)                    -> return_true / return_false  ; "visitor walkby"


; Visitor main — direct port of npc_service_semiglobal "SG - Functional - Main"
; (BHAV 8193), which is the canonical NPC idle loop. The player avatar template
; (templateperson.iff BHAV 4096) uses the same shape.
;
; Why not just call do idle core (0x2025) directly from main? Because the queue
; check inside Wait for Notify kills idle the moment any action is queued:
; idle_for_input (from main, allow_push=false in the Wait-for-Notify path) sets
; NotifiedByIdleForInput when Queue.Count>0, and Wait for Notify reads that flag
; and returns true, exiting idle BEFORE reaching the animate primitive
; (VMIdleForInput.cs:24-28 + VMEntity.cs:1745). Result: zero animation, just a
; tight main loop.
;
; The fix is to push "Internal Idle" (TTAB idx 6 -> BHAV 4107) onto self at Idle
; priority and let idle_for_input here pop the queue. Inside the resulting
; ActionTree, idle_for_input's queue-check is gated on NotifyIdle (default false)
; so 0x2025's animate primitives actually run.
BHAV [4106] "visitor main" 0x8003 args=0 locals=1
boot:
  StackObjID[0] := MyObj[11]                                     -> init_check / init_check  ; StackObj=self (push_interaction's EnqueueAction reads StackObject.Thread)
init_check:
  MyAttrs[0] > 0                                                 -> top / start_timer  ; first run? then prime counter
start_timer:
  MyAttrs[0] := 30                                               -> top / top          ; ~30 idle cycles before autonomous walk-off
top:
  MyAttrs[0] -= 1                                                -> check_done / check_done
check_done:
  MyAttrs[0] > 0                                                 -> idle_setup / push_walkby  ; timer not yet expired -> idle; else walk off
idle_setup:
  Local[0] := MyObj[11]                                          -> push / push     ; capture self id for ObjInLocal push
push:
  push_interaction raw=[06,00,06,02,00,00,00,00]                 -> reset / reset   ; idx=6 Internal Idle, priority=Idle, obj=Local[0]
reset:
  MyPerson[33] := 0                                              -> args / args     ; clear NPC priority so AttemptPush dispatches
args:
  Params[0] := 1                                                 -> wait / wait
wait:
  idle_for_input dec=Temps[0] allow_push=true                    -> tick / error    ; AttemptPush + yield
tick:
  Temps[0] := 1                                                  -> sleep / sleep
sleep:
  call_global 0x0118 args=(1,0,0,0)                              -> init_check / init_check  ; brief yield, then next iteration
push_walkby:
  Local[0] := MyObj[11]                                          -> walkby / walkby
walkby:
  push_interaction raw=[05,00,01,02,00,00,00,00]                 -> wait_gone / wait_gone  ; idx=5 Immediate Walkby, priority=Max, obj=Local[0]
wait_gone:
  call_global 0x0118 args=(60,0,0,0)                             -> wait_gone / wait_gone  ; yield until walkby removes us


; Internal Idle — the action body main pushes onto self at Idle priority.
; Mirrors PersonGlobals "Interaction - Idle" (BHAV 8622). Runs inside an ActionTree
; so the queue-check in idle_for_input is gated on NotifyIdle (default false) —
; do idle core gets to the animate primitives and the neutral standing idle anims
; (global.iff STR# 156 ids 235/242/309) actually play. Also drives "look at
; something" (0x2063) via 0x2025's call chain.
BHAV [4107] "Internal Idle" 0x8003 args=0 locals=1
setup:
  StackObjID[0] := MyObj[11]                                     -> idle / error    ; ensure StackObj points at self
idle:
  call_semiglobal 0x2025 args=(1,1,0,0)                          -> reset / reset   ; "do idle core"
reset:
  call_semiglobal 0x2064 args=(0,0,0,0)                          -> return_true / return_true  ; "reset idle" — clears idle state on exit


; Controller init — sleep 0 ticks; main fires immediately.
BHAV [5000] "controller init" 0x8003 args=0 locals=0
  sleep ticks=Temps[0]                                  -> return_true / return_true


; Controller main: spawn upfront, then loop with a ~60s wait between visitors.
; Each iteration:
;   1. picks a sidewalk portal,
;   2. asks the server for a city sim (async — caches identity on this thread),
;   3. creates the visitor object,
;   4. applies the cached identity in the same tick (no default-look flash),
;   5. places the visitor along the portal vector.
;   Local[1] = chosen portal ID
BHAV [5001] "controller main" 0x8003 args=0 locals=3
loop_top:
  StackObjID[0] := 0                                    -> pick_side / error
pick_side:
  random_number raw=[01,00,08,00,02,00,07,00]           -> pick_branch / error
pick_branch:
  Temps[1] == 0                                         -> portal_left / portal_right
portal_left:
  set_to_next raw=[34,20,BC,23,84,0A,00,00]             -> save_portal / wait_loop
portal_right:
  set_to_next raw=[80,C3,57,4E,84,0A,00,00]             -> save_portal / wait_loop
save_portal:
  Local[1] := StackObjID[0]                             -> request_identity / error
request_identity:
  TempXL[0] := 0                                        -> fetch_identity / fetch_identity
fetch_identity:
  generic_call mode=143                                 -> spawn_visitor / wait_loop
spawn_visitor:
  create_object raw=[61,1E,C8,A0,06,01,00,00]           -> save_visitor / wait_loop
save_visitor:
  Local[2] := StackObjID[0]                             -> apply_identity / error
apply_identity:
  generic_call mode=144                                 -> place_vector / place_vector
place_vector:
  find_location_for raw=[03,01,01,00,00,00,00,00]       -> wait_loop / place_radius
place_radius:
  find_location_for raw=[00,01,01,00,00,00,00,00]       -> wait_loop / drop_visitor
drop_visitor:
  remove_object raw=[01,00,00,00,00,00,00,00]           -> wait_loop / wait_loop
wait_loop:
  call_global 0x0118 args=(1800,0,0,0)                  -> loop_top / loop_top
; (The Autonomous push of Delayed Walkby was removed: at priority 2 it blocks the
; Idle-priority Internal Idle the visitor's main pushes, and would also freeze the
; visitor for ~60s during its sleep. Walk-off is now driven from the visitor's own
; main loop / Say Goodbye pie interaction; a timer-based autonomous walk-off can
; be re-added later using a MyAttrs counter inside main.)
