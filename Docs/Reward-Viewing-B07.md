# B07 reward presentation

Chest images now interpolate premultiplied RGBA into one reusable output and draw once against the background. Thirteen lazily generated 256px packed source frames, two output textures and one upload buffer bound ownership; a quantized unchanged sample does not upload again. This is a correctness/ownership change, not a measured Unity frame-rate improvement. The selected chest moves from its captured choice rectangle to the final artwork rectangle; skip uses the same final receipt/position.

The durable chest receipt records actual gold and thread gains, including currency caps. Legacy receipts explicitly lack actual deltas instead of guessing them. First collections offer “accept and try on”; acknowledgement must save before opening the trial page. Duplicate and gold results keep their ordinary acknowledgement. Legendary-choice progress uses the real balance and configured cost.

Two trial slots, viewing composition and each of three manual angles are separate bounded state. Full/weapon/back switches retain trial appearances and angle history. Pausing or disabling releases only model resources; changing owner/leaving collection resets the presentation. Trials never equip items or change statistics.

Validation: ChestCurrencyDeltaTests (real save failures/caps/restart), RewardViewingRulesTests (pixels/endpoints/independent views), ChestCompositeProductionTests.py (actual draw/cache/release methods plus double-alpha negative control), ChestTrialProductionTests.py (actual transition and service acknowledgement with ignored-save-failure negative control), existing chest receipt and collection source regressions. Unity 6 rendering, perceptual smoothness and device timing require separate runtime validation.
