; Visitor init — mirrors npc_fso_witch BHAV 4096:
; call PersonGlobals "init NPC" (0x206A) which wraps Init Person and restores
; PersonType=2 (Service NPC). Avoids the npc_service_semiglobal wrapper.
BHAV [4097] "visitor init" 0x8003 args=0 locals=0
  call_semiglobal 0x206A args=(0,0,0,0)                 -> return_true / return_true


; Visitor "Walk Off Lot" — routes the visitor near a sidewalk portal then deletes
; the visitor object. Used as:
;   - autonomous walk-off (main BHAV 4106 call_private 0x1002 when timer expires)
;   - user-driven via Say Goodbye -> TTAB idx 5 push_interaction at Max priority.
;
; Why goto_relative instead of goto_routing_slot? goto_routing_slot reads
; StackObject.Slots.Slots[3][data] (VMMemory.GetSlot:743) and NREs if the target
; has no SLOT chunk. Sidewalk portals (GUID 0x81E6BEF9) don't declare slots.
; goto_relative builds a slot dynamically (VMGotoRelativePosition.cs:24-43), so
; it works for any target. Same primitive Helper - Approach Visitor uses.
;
; Why not call generic_call mode=25 (LeaveLot)? It despawns engine-side without
; routing AND emits a hardcoded "X has left the lot" chat event in
; LotServerGlobalLink.LeaveLot — not appropriate for autonomous walkbys.
BHAV [4098] "visitor walkby" 0x8003 args=0 locals=0
find:
  call_global 0x0175 args=(0,0,0,0)                     -> check / die  ; Temps[0] := random sidewalk portal (GUID 0x81E6BEF9)
check:
  Temps[0] == 0                                         -> die / grab  ; defensive: no portal -> just remove
grab:
  StackObjID[0] := Temps[0]                             -> route / die  ; StackObject := portal
route:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt  -> die / die  ; walk to a tile near the portal
die:
  remove_object target=0                                -> return_true / return_true  ; delete Caller (= visitor)


; Check tree for hidden interactions (TTAB idx 5 Immediate Walkby + idx 6 Internal
; Idle). Canonical TSO pattern for "hide from pie but allow programmatic push":
;
;   - During pie-menu build (VMEntity.GetPieMenu), the test runs with Caller=player
;     (the clicker). VMEntity:1016-1020 checks caller.ObjectData[HideInteraction]
;     after the test and skips the pie entry if it's set.
;   - During VMThread.AttemptPush, the test must return RETURN_TRUE; if it returns
;     RETURN_FALSE the action is silently removed from the queue (AttemptPush:225)
;     and never dispatches.
;
; So we set MyObj[50] (HideInteraction) := 1 and return_true: pie hides the entry,
; AttemptPush sees TRUE and dispatches.
BHAV [4099] "walkby pie hide" 0x8003 args=0 locals=0
  MyObj[50] := 1                                        -> return_true / return_true


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
  StackObjID[0] := MyObj[11]                                     -> check_init / check_init  ; StackObj=self for push_interaction (push_interaction enqueues on StackObject.Thread)
check_init:
  MyAttrs[0] > 0                                                 -> tick / init_timer        ; first run? prime counter
init_timer:
  MyAttrs[0] := 30                                               -> tick / tick              ; ~30 idle cycles before autonomous walk-off
tick:
  MyAttrs[0] -= 1                                                -> decide / decide
decide:
  MyAttrs[0] > 0                                                 -> push_idle / walkoff      ; timer not yet expired -> idle; else walk off (synchronous direct call)
walkoff:
  call_private 0x1002 args=(0,0,0,0)                             -> wait_gone / wait_gone    ; BHAV 4098 "visitor walkby" — runs in our context (Caller=self), so remove_object target=0 removes us. No TTAB indirection needed.
wait_gone:
  call_global 0x0118 args=(60,0,0,0)                             -> wait_gone / wait_gone    ; only reached if walkby returned without removing (no portal); spin
push_idle:
  Local[0] := MyObj[11]                                          -> idle_push / idle_push    ; self id for ObjInLocal push
idle_push:
  push_interaction raw=[06,00,06,02,00,00,00,00]                 -> reset / reset            ; idx=6 Internal Idle, Idle priority
reset:
  MyPerson[33] := 0                                              -> args / args              ; clear NPC priority so AttemptPush dispatches
args:
  Params[0] := 1                                                 -> wait / wait
wait:
  idle_for_input dec=Temps[0] allow_push=true                    -> next / error             ; AttemptPush dispatches the pushed Internal Idle (idx 6)
next:
  Temps[0] := 1                                                  -> sleep / sleep
sleep:
  call_global 0x0118 args=(1,0,0,0)                              -> tick / tick              ; brief yield then loop. Goes to tick (not check_init) so counter doesn't reset.


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


; Controller init — sleep ~2 seconds (60 ticks at 30/s) before main starts.
; This is critical for the city-avatar identity picker. RequestCityAvatar in
; LotServerGlobalLink filters eligible avatars against vm.Context.ObjectQueries
; .Avatars (line 1334) — i.e. avatars currently registered on the lot. At lot
; bootstrap the controller's main would fire before VMNetSimJoinCmd has created
; the joining player's avatar, so the filter sees an empty set and the player's
; own PersistID is eligible to be assigned to a spawned visitor. When the player
; then joins, both avatars share a PersistID; AvatarsByPersist collisions cause
; the player's UI to flicker to the visitor's caretaker outfit, and deleting the
; visitor (its walkby) evicts the player's dict entry → kicked / lot crashes.
; The 60-tick wait gives the join command time to be processed first.
BHAV [5000] "controller init" 0x8003 args=0 locals=0
  call_global 0x0118 args=(60,0,0,0)                    -> return_true / return_true  ; "Idle" — sleep 60 ticks


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
