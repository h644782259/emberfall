#!/usr/bin/env python3
"""Summarize an actual Unity JSONL event file; never invent missing events or a pass verdict."""
import argparse
import collections
import json
import re
from pathlib import Path


def normalize_object_id(value):
    """Keep v2 full-width decimal strings; also accept exact legacy signed int IDs."""
    if isinstance(value, bool) or not isinstance(value, (str, int)):
        raise ValueError('Object IDs must be decimal strings or legacy integers, never floats')
    text = str(value)
    if not re.fullmatch(r'0|[1-9]\d*|-[1-9]\d*', text):
        raise ValueError('Invalid object ID')
    number = int(text)
    if not -(1 << 31) <= number <= (1 << 64) - 1:
        raise ValueError('Object ID outside supported range')
    return text


def summarize(path):
    counts = collections.Counter()
    damage = 0.0
    no_enemy_hit = 0
    first = last = None
    for line_number, line in enumerate(Path(path).open(encoding='utf-8'), 1):
        if not line.strip():
            continue
        row = json.loads(line)
        if not isinstance(row, dict) or 'kind' not in row or 'frame' not in row:
            raise ValueError(f'Invalid event at line {line_number}')
        for key in ('actorId', 'targetId'):
            if key in row:
                row[key] = normalize_object_id(row[key])
        counts[row['kind']] += 1
        if row['kind'] == 'projectileend' and re.fullmatch(r'-?\d+:(expired|terrain):hits=0', row.get('detail', '')):
            no_enemy_hit += 1
        first = row['frame'] if first is None else min(first, row['frame'])
        last = row['frame'] if last is None else max(last, row['frame'])
        # Enemy health-loss callback only. Projectile hits and sampled HP are not added again.
        if row['kind'] == 'damage':
            damage += float(row.get('amount', 0))
    return {'source': str(Path(path).resolve()), 'scope': 'Recorded events only; no gameplay or visual acceptance verdict',
            'first_frame': first, 'last_frame': last, 'event_counts': dict(sorted(counts.items())),
            'recorded_damage_amount': damage,
            'derived_projectile_no_enemy_hit': no_enemy_hit,
            'miss_definition': 'Only projectileend expired/terrain with hits=0; excludes retired/disposed. May have hit props. Not whole-attack, melee or skill miss rate.',
            'unobserved_event_kinds': [k for k in ('damage', 'projectilehit', 'projectileend', 'chargecancel', 'noenergy', 'takendamage', 'petchase', 'death') if not counts[k]],
            'limitations': 'Unobserved can mean untested, absent hooks, cancelled actions, or no matching event; it is not zero-failure evidence. Read raw event detail/reason and hook coverage. Sampled HP loss is excluded.'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('events')
    args = parser.parse_args()
    print(json.dumps(summarize(args.events), ensure_ascii=False, indent=2))
