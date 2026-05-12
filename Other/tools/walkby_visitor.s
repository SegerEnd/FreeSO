; Visitor init — mirrors npc_fso_pizzaman BHAV 4096:
; call semi-global "Init Person" (0x2000) via npc_service_semiglobal.
BHAV [4097] "visitor init" 0x8003 args=0 locals=0
  call_semiglobal 0x2000 args=(0,0,0,0)                 -> return_true / return_true


; Visitor "Walk Off Lot" interaction (registered in TTAB index 0).
; Pushed onto the visitor by the controller after placement; runs via PersonGlobals
; "person main" (8193). Uses global 0x0175 "find random portal" instead of hand-rolled
; portal lookup so the same code works on any lot regardless of sidewalk GUID layout.
BHAV [4098] "visitor walkby" 0x8003 args=0 locals=0
find:
  call_global 0x0175 args=(0,0,0,0)                     -> grab / die  ; Temps[0] := random portal
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
; play the standard chat animation. Mirrors npc_fso_witch's "Helper - Talk Core".
BHAV [4100] "Helper - Approach Visitor" 0x8003 args=0 locals=0
approach:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt   -> play_anim / return_false
play_anim:
  animate id=1 source=Object event=0 expected_evts=0 flags=Play  -> return_true / play_anim


; Pie-menu interactions. Each is a thin wrapper around the approach helper so adding
; more is just one TTAB row + one BHAV stub. Visually similar today (chat animation);
; differentiate later by swapping animation id or adding a dialog string.
BHAV [4101] "Interaction - Wave" 0x8003 args=0 locals=0
  call_private 0x1004 args=(0,0,0,0)                    -> return_true / return_false

BHAV [4102] "Interaction - Greet" 0x8003 args=0 locals=0
  call_private 0x1004 args=(0,0,0,0)                    -> return_true / return_false

BHAV [4103] "Interaction - Compliment" 0x8003 args=0 locals=0
  call_private 0x1004 args=(0,0,0,0)                    -> return_true / return_false


; User-facing "Say Goodbye": immediate walkby (no delay). Same walking BHAV as the
; autonomous path — just skips the wait so the visitor leaves when the player asks.
BHAV [4104] "Interaction - Say Goodbye" 0x8003 args=0 locals=0
  call_private 0x1002 args=(0,0,0,0)                    -> return_true / return_false  ; "visitor walkby"


; Delayed walkby: hangs around ~60s of real time (1800 ticks at 30 ticks/sec) so
; the visitor isn't just instantly leaving, then runs the walkby. Pushed by the
; controller right after placement. A user-clicked "Say Goodbye" preempts this via
; higher priority (Max vs the controller's Autonomous-priority push).
BHAV [4105] "Delayed Walkby" 0x8003 args=0 locals=0
  call_global 0x0118 args=(1800,0,0,0)                  -> walkby / walkby  ; "Wait for Notify" / sleep
walkby:
  call_private 0x1002 args=(0,0,0,0)                    -> return_true / return_false  ; "visitor walkby"


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
  find_location_for raw=[03,01,01,00,00,00,00,00]       -> push_walkby / place_radius
place_radius:
  find_location_for raw=[00,01,01,00,00,00,00,00]       -> push_walkby / drop_visitor
push_walkby:
  push_interaction raw=[00,02,02,02,00,00,00,00]        -> wait_loop / wait_loop  ; interaction=0 obj=Local[2] priority=Autonomous flags=ObjInLocal
drop_visitor:
  remove_object raw=[01,00,00,00,00,00,00,00]           -> wait_loop / wait_loop
wait_loop:
  call_global 0x0118 args=(1800,0,0,0)                  -> loop_top / loop_top
