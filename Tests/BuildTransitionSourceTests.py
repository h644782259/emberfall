#!/usr/bin/env python3
"""Companion build wiring contracts; no Unity runtime is executed here."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda path:(root/path).read_text()
pet=read('Assets/Scripts/Combat/SummonedCompanion.cs');player=read('Assets/Scripts/Combat/PlayerController.cs')
fixture=read('Assets/Editor/CompanionBuildValidation.cs');runner=read('Assets/Editor/SummonerValidation.cs')
def body(source, signature):
 start=source.index('{', source.index(signature)); depth=1
 for end in range(start+1,len(source)):
  depth+=(source[end]=='{')-(source[end]=='}')
  if depth==0:return source[start+1:end]
 raise AssertionError(signature)
checks=[]
def check(ok,why):
 if not ok:raise AssertionError(why)
 checks.append(why)
refresh=body(pet,'public static void RefreshBuild')
check(refresh.index('EnforceCapacity(owner)')<refresh.index('pet.RefreshPower(false)'), 'route/cap are reconciled before stats')
check('pet.Owner == owner && pet.IsAlive' in refresh and 'owner.HeroClass != HeroClass.Summoner' in refresh,'refresh isolates living companions of the correct summoner')
check('SummonedCompanion.RefreshBuild(this)' in body(player,'public void RefreshStats'), 'successful profile events reach companion reconciliation without waiting for Update')
teleport=body(player,'public void Teleport')
check(teleport.index('SummonedCompanion.RefreshBuild(this)')<teleport.index('CombatEpoch++')<teleport.index('TransferPermanentPartners'), 'pre-transfer route reconciliation sees the old living epoch')
power=body(pet,'private void RefreshPower')
check('CompanionRules.EffectiveRank((int)Form, IsStarter, IsPermanent, rank, learned[2], learned[4])' in power, 'persistent power is derived from current learned wolf/spirit investment')
check('CompanionRules.PreserveRecastHealth(Health, maximum)' in power and 'Health /' not in power, 'rank and equipment refresh preserve absolute HP instead of multiplying it')
check('if (commandTime > 0) commandMultiplier = CompanionRules.ActiveCommandMultiplier(rank, commandEmpowered)' in power, 'remaining command strength tracks changed rank without discarding empowerment')
for token in ['TryConsume(', 'cooldown =', 'commandTime =', 'recallTime =', 'RemainingLifetime =', 'Commands.Grant']:
 check(token not in power and token not in refresh,'stat refresh does not mutate '+token)
capacity=body(pet,'public static void EnforceCapacity')
check(capacity.index('pet.RefreshPower(false)')<capacity.index('pet.IsPermanent = false'),'simultaneous rank downgrade and Pack route switch reconciles before demotion')
check('CompanionBuildValidation.Validate(game, check)' in runner and 'RequireIsolatedRuntime' in fixture, 'prepared engine fixture is dispatched under isolated-save checks')
for action in ['p.RefundSkillRanks(true)','p.ResetBuild(true)','p.ApplyBuildPreset(0, true)','p.LearnSkill(4)','p.Equip(twin.id)','player.Teleport(position)']:
 check(action in fixture,'prepared engine fixture covers '+action)
check('game.SetPaused(true)' in fixture and 'runtime.Remaining(4) == skillCooldown' in fixture and 'opportunity' in fixture,'fixture covers paused changes, skill recovery and command tokens')
print('PASS:',len(checks),'companion build source contracts (not Unity execution)')
