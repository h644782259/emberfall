# Locomotion and guardian animation sample support

Hero gait consumes accepted horizontal walking displacement from the normal navigation call. Input magnitude no longer drives stepping into a wall. The sample is corrected for the arena boundary, transformed into the actor's final local facing, and used for forward, backward and lateral leg motion. Jumping has independent knee tuck and a finite landing compression. Teleport resets the pose state; blink, skill displacement and external knockback never become walking distance samples.

LocomotionPoseState integrates distance into step phase, smooths velocity/start-stop response and supplies bounded cloth inertia. The cloth uses its existing 98-vertex buffers, with pinned shoulder vertices and at most 0.088m added backward/forward and 0.048m lateral deformation. Paused time does not advance state. The same hero state supports the Vanguard and Ranger rigs; their weapon/upper-body posing remains class-specific.

Articulated enemies receive accepted navigation movement separately from knockback, pull, charge and attack lunges. Their controller sends explicit windup and recovery progress. The real ResolveAttack transition now selects the contact pose before that frame is rendered; the old smoothed .95/1 inference is removed. Guardian windup, contact and recovery use the existing warning and attack cadence. Pet-specific pose branches are preserved.

Fixed equipment/camera/resolution/quality footage and a whole-scene HUD readability review remain required in a working Unity runtime.

Validation checks cover pure displacement/jump/inertia/phase rules, source wiring and basic/combat/opportunity contracts. These checks do not establish foot planting, visual polish, animation quality, Unity 6 execution, touch usability or real-device performance.