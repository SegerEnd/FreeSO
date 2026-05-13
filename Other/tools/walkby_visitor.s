; Init: PersonGlobals "init NPC" sets PersonType + base person data. VMAvatar
; SetMotiveData (VMAvatar.cs:995) clamps motives to [-100, max(old, tuning_limit
; ?? 100)], so write ~mid values, not 1000s. Goal: spread "wants" across all
; motives so find_best_action has real signal to score with. Permission ACL
; (VMThread.cs:1009-1040) still blocks anything not tagged AllowVisitors, so
; on residential lots the eligible set will skew maid-chore-shaped regardless.
BHAV [4097] "visitor init" 0x8003 args=0 locals=0
  call_semiglobal 0x206A args=(0,0,0,0)                 -> social / social
social:
  MyMotives[14] := 50                                   -> fun / fun
fun:
  MyMotives[15] := 50                                   -> comfort / comfort
comfort:
  MyMotives[6] := 50                                    -> energy / energy
energy:
  MyMotives[5] := 80                                    -> hunger / hunger
hunger:
  MyMotives[7] := 70                                    -> hygiene / hygiene
hygiene:
  MyMotives[8] := 80                                    -> bladder / bladder
bladder:
  MyMotives[9] := 80                                    -> room / room
room:
  MyMotives[13] := 60                                   -> mood / mood
mood:
  MyMotives[3] := 70                                    -> return_true / return_true


; Walk to the *opposite* walkby portal from where we spawned, then despawn.
; Controller writes spawn-side flag into MyAttrs[2]: 0=left, 1=right.
; goto_relative builds a routing slot dynamically — portals have no SLOT chunk.
BHAV [4098] "visitor walkby" 0x8003 args=0 locals=0
clear_stack:
  StackObjID[0] := 0                                    -> direction / direction
direction:
  MyAttrs[2] == 0                                       -> walk_right / walk_left
walk_right:
  set_to_next raw=[80,C3,57,4E,84,0A,00,00]             -> route / die  ; OfType 0x4E57C380
walk_left:
  set_to_next raw=[34,20,BC,23,84,0A,00,00]             -> route / die  ; OfType 0x23BC2034
route:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt  -> die / return_false  ; route interrupted (e.g. social pushed) -> stay; reached portal -> die
die:
  remove_object target=0                                -> return_true / return_true


; Hide-from-pie test for TTAB idx 5/6. AttemptPush also runs the test and drops
; the action on RETURN_FALSE, so we set HideInteraction and return TRUE.
BHAV [4099] "walkby pie hide" 0x8003 args=0 locals=0
  MyObj[50] := 1                                        -> return_true / return_true


; Pie socials: bump visitor linger, push Receive Social on visitor at UserDriven,
; walk over, then notify_out_of_idle so visitor's Wait for Notify wakes — both
; animate in sync (canonical 2-sim pattern, see PG 8654 carrying put-down).
BHAV [4101] "Interaction - Wave" 0x8003 args=0 locals=1
  StackAttrs[0] := 50                                   -> save_visitor / save_visitor  ; visit duration (main-loop iters)
save_visitor:
  Local[0] := StackObjID[0]                             -> push_react / push_react  ; push_interaction reads Local[0]
push_react:
  push_interaction raw=[07,00,03,02,00,00,00,00]        -> approach / approach  ; idx=7 UserDriven on Local[0]
approach:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt  -> notify / return_false
notify:
  stackobj_notify_out_of_idle raw=[FF,FF,FF,FF,FF,FF,FF,FF]  -> anim / anim
anim:
  animate raw=[8A,01,00,00,03,00,00,00]                 -> return_true / anim  ; id=394 source=Misc

BHAV [4102] "Interaction - Greet" 0x8003 args=0 locals=1
  StackAttrs[0] := 50                                   -> save_visitor / save_visitor
save_visitor:
  Local[0] := StackObjID[0]                             -> push_react / push_react
push_react:
  push_interaction raw=[07,00,03,02,00,00,00,00]        -> approach / approach
approach:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt  -> notify / return_false
notify:
  stackobj_notify_out_of_idle raw=[FF,FF,FF,FF,FF,FF,FF,FF]  -> anim / anim
anim:
  animate raw=[8A,01,00,00,03,00,00,00]                 -> return_true / anim

BHAV [4103] "Interaction - Compliment" 0x8003 args=0 locals=1
  StackAttrs[0] := 50                                   -> save_visitor / save_visitor
save_visitor:
  Local[0] := StackObjID[0]                             -> push_react / push_react
push_react:
  push_interaction raw=[07,00,03,02,00,00,00,00]        -> approach / approach
approach:
  goto_relative loc=AnywhereNear dir=Facing flags=AllowDiffAlt  -> notify / return_false
notify:
  stackobj_notify_out_of_idle raw=[FF,FF,FF,FF,FF,FF,FF,FF]  -> anim / anim
anim:
  animate raw=[8A,01,00,00,03,00,00,00]                 -> return_true / anim


; Pushes the hidden Immediate Walkby (idx 5 -> BHAV 4098) onto the visitor at
; Maximum priority. Can't call_private 4098 inline — caller is the player.
BHAV [4104] "Interaction - Say Goodbye" 0x8003 args=0 locals=1
  Local[0] := StackObjID[0]                             -> push / return_false
push:
  push_interaction raw=[05,00,01,02,00,00,00,00]        -> return_true / return_false  ; idx=5 priority=Max


; Dead code (kept to preserve TTAB idx 0 layout). Was the autonomous Delayed
; Walkby push from the controller; main now handles walk-off via its own loop.
BHAV [4105] "Delayed Walkby" 0x8003 args=0 locals=0
  call_global 0x0118 args=(1800,0,0,0)                  -> walkby / walkby
walkby:
  call_private 0x1002 args=(0,0,0,0)                    -> return_true / return_false


; Main: default behaviour is the cross-lot walkby. Once greeted (MyAttrs[0]>0,
; set by a social), tick down and try find_best_action. The autonomy scorer
; only sees AllowVisitors-tagged interactions (mostly chores), so the visitor
; behaves maid-flavoured on a residential lot — that's a TSO permission-ACL
; limit, not a tuning one. On TRUE the chore is queued at Autonomous and runs
; via idle_for_input handover; on FALSE we push Internal Idle so the visitor
; head-tracks + animates instead of standing inert.
BHAV [4106] "visitor main" 0x8003 args=0 locals=1
boot:
  StackObjID[0] := MyObj[11]                            -> top / top
top:
  MyAttrs[0] > 0                                        -> tick_visit / push_walkby
tick_visit:
  MyAttrs[0] -= 1                                       -> try_auto / try_auto
try_auto:
  find_best_action raw=[00,00,00,00,00,00,00,00]        -> reset / fallback_idle
fallback_idle:
  Local[0] := MyObj[11]                                 -> idle_push / idle_push
idle_push:
  push_interaction raw=[06,00,06,02,00,00,00,00]        -> reset / reset  ; idx=6 Internal Idle
push_walkby:
  Local[0] := MyObj[11]                                 -> walkby_push / walkby_push
walkby_push:
  push_interaction raw=[05,00,06,02,00,00,00,00]        -> reset / reset  ; idx=5 Immediate Walkby
reset:
  MyPerson[33] := 0                                     -> args / args
args:
  Params[0] := 1                                        -> wait / wait
wait:
  idle_for_input dec=Temps[0] allow_push=true           -> next / error
next:
  Temps[0] := 1                                         -> sleep / sleep
sleep:
  call_global 0x0118 args=(1,0,0,0)                     -> top / top


; Body of idx 6. Inside an action tree so do idle core's animate calls actually run.
BHAV [4107] "Internal Idle" 0x8003 args=0 locals=1
setup:
  StackObjID[0] := MyObj[11]                            -> idle / error
idle:
  call_semiglobal 0x2025 args=(1,1,0,0)                 -> reset / reset
reset:
  call_semiglobal 0x2064 args=(0,0,0,0)                 -> return_true / return_true


; Body of idx 7 — visitor side of social handshake. Same nearest-person scan as
; witch's "Helper - Look at someone nearby" (NPC fso_witch BHAV 4110): iterate
; Persons in same room, pick closest, head-track them (engine head-tracks for
; MyPerson[45] ticks). Then yield via Wait for Notify (global 0x0119) until the
; caller's notify_out_of_idle fires. Temps[0] supplies a timeout ceiling so we
; never deadlock if the caller never arrives.
BHAV [4108] "Receive Social" 0x8003 args=0 locals=3
init_room:
  Local[0] := MyObj[29]                                 -> init_min / init_min
init_min:
  Local[1] := 100                                       -> init_pick / init_pick
init_pick:
  Local[2] := 0                                         -> iter_init / iter_init
iter_init:
  StackObjID[0] := 0                                    -> iter / iter
iter:
  set_to_next raw=[00,00,00,00,81,0A,00,00]             -> filter_self / pick  ; Person iter (witch 4110)
filter_self:
  StackObjID[0] == MyObj[11]                            -> iter / filter_room
filter_room:
  StackObj[29] == Local[0]                              -> measure / iter
measure:
  get_distance_to raw=[00,00,00,00,00,00,00,00]         -> check_closer / iter
check_closer:
  Temps[0] < Local[1]                                   -> update_min / iter
update_min:
  Local[1] := Temps[0]                                  -> save_pick / save_pick
save_pick:
  Local[2] := StackObjID[0]                             -> iter / iter
pick:
  StackObjID[0] := Local[2]                             -> check_found / check_found
check_found:
  StackObjID[0] == 0                                    -> return_true / face
face:
  look_towards raw=[02,00,00,00,00,00,00,00]            -> head / head  ; body turn yields
head:
  look_towards raw=[00,00,00,00,00,00,00,00]            -> track / track  ; head seek
track:
  MyPerson[45] := 300                                   -> set_timeout / set_timeout  ; head-track ~10s (witch 4110)
set_timeout:
  Temps[0] := 300                                       -> wait / wait
wait:
  call_global 0x0119 args=(0,0,0,0)                     -> anim / anim  ; Wait for Notify (no-push)
anim:
  animate raw=[8A,01,00,00,03,00,00,00]                 -> return_true / anim  ; id=394 compliment-stand


; Sleep before main starts so VMNetSimJoinCmd has time to register the joining
; player avatar — RequestCityAvatar's filter (LotServerGlobalLink:1334) reads
; vm.Context.ObjectQueries.Avatars.
BHAV [5000] "controller init" 0x8003 args=0 locals=0
  call_global 0x0118 args=(60,0,0,0)                    -> return_true / return_true


; Pick a portal side, fetch city avatar identity, spawn visitor at that portal,
; apply the cached identity, and record the spawn side on the visitor. Loop.
BHAV [5001] "controller main" 0x8003 args=0 locals=3
loop_top:
  StackObjID[0] := 0                                    -> pick_side / error
pick_side:
  random_number raw=[01,00,08,00,02,00,07,00]           -> pick_branch / error
pick_branch:
  Temps[1] == 0                                         -> portal_left / portal_right
portal_left:
  set_to_next raw=[34,20,BC,23,84,0A,00,00]             -> save_portal / wait_loop  ; 0x23BC2034
portal_right:
  set_to_next raw=[80,C3,57,4E,84,0A,00,00]             -> save_portal / wait_loop  ; 0x4E57C380
save_portal:
  Local[1] := StackObjID[0]                             -> request_identity / error
request_identity:
  TempXL[0] := 0                                        -> fetch_identity / fetch_identity
fetch_identity:
  generic_call mode=143                                 -> spawn_visitor / wait_loop
spawn_visitor:
  create_object raw=[61,1E,C8,A0,06,01,00,00]           -> save_visitor / wait_loop  ; 0xA0C81E61
save_visitor:
  Local[2] := StackObjID[0]                             -> save_direction / error
save_direction:
  StackAttrs[2] := Temps[1]                             -> apply_identity / apply_identity  ; visitor.MyAttrs[2] = spawn side (0=left,1=right)
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
