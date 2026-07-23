"""Generate the Alchemist character select sound.

The clip is a potion brew: a plop (the ingredient drop), a flame-like
crackle that blooms and fades on a bell curve, then soft bubbling. All
of it is synthesized. No game audio is used. The crackle band balance
(73/25/2 low/mid/high) approximates the measured balance of the base
game flame sfx (78/20/2).

The output goes into the Godot project, so the pck carries it. BaseLib
routes a res:// sfx path through Godot audio players (PlayResourcePatch),
so FMOD never sees this file.

Run from the repo root:
    python3 scripts/gen_select_sfx.py
Then run scripts/dev.sh publish to install it.
"""
import math
import os
import random
import struct
import wave

SR = 48000
OUT = os.path.join(os.path.dirname(__file__), "..", "Alchemist", "audio", "alchemist_select.wav")


def zeros(n):
    return [0.0] * n


def add(buf, start, samples, gain=1.0):
    for i, s in enumerate(samples):
        j = start + i
        if 0 <= j < len(buf):
            buf[j] += s * gain


def bubble(f0, f_rise, dur, decay=6.0):
    """One bubble: a sine chirp that rises in pitch and decays (Farnell model)"""
    n = int(dur * SR)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / n
        f = f0 * (1.0 + f_rise * t * t)
        phase += 2 * math.pi * f / SR
        env = math.exp(-decay * t) * math.sin(math.pi * min(1.0, t * 12))
        out.append(math.sin(phase) * env)
    return out


def noise_band(n, lp, hp=0.0, rng=None):
    """Filtered noise: one-pole lowpass, then an optional one-pole highpass"""
    rng = rng or random
    out = zeros(n)
    lo = 0.0
    hp_state = 0.0
    for i in range(n):
        w = rng.uniform(-1, 1)
        lo += lp * (w - lo)
        v = lo
        if hp > 0:
            hp_state += hp * (v - hp_state)
            v = v - hp_state
        out[i] = v
    return out


def crackle(n, rng, pop_rate=70.0):
    """Flame texture: a dark noise roar plus sparse bright micro-pops"""
    roar = noise_band(n, lp=0.045, rng=rng)
    out = [r * 1.6 for r in roar]
    t = 0.0
    while True:
        t += rng.expovariate(pop_rate)
        s0 = int(t * SR)
        if s0 >= n:
            break
        plen = int(rng.uniform(0.001, 0.006) * SR)
        g = rng.uniform(0.05, 0.30)
        for i in range(min(plen, n - s0)):
            out[s0 + i] += rng.uniform(-1, 1) * g * math.exp(-5.0 * i / plen)
    # shave the remaining highs toward the flame balance
    lo = 0.0
    for i in range(n):
        lo += 0.30 * (out[i] - lo)
        out[i] = lo
    peak = max(abs(s) for s in out) or 1.0
    return [s / peak for s in out]


def render(name, seed=99, dur=2.2, bubble_rate=18, bubble_gain=0.45,
           simmer_gain=0.16, f_lo=220, f_hi=560, bubble_start=0.48,
           bubble_sz=(0.07, 0.16), bubble_decay=3.5, crackle_gain=0.40,
           bell_center=0.30, bell_sigma=0.17, drive=1.8):
    rng = random.Random(seed)
    n = int(dur * SR)
    buf = zeros(n)

    # the opening plop: one big low bubble, the ingredient drop
    add(buf, int(0.04 * SR), bubble(85, 1.6, 0.13), 1.4 * bubble_gain)

    # bubbles: Poisson scatter with pitch and size variation
    t = bubble_start
    while t < dur - 0.15:
        t += rng.expovariate(bubble_rate)
        f0 = rng.uniform(f_lo, f_hi)
        size = rng.uniform(*bubble_sz)
        g = rng.uniform(0.4, 1.0) * bubble_gain
        add(buf, int(t * SR), bubble(f0, rng.uniform(1.2, 2.2), size, decay=bubble_decay), g)

    # simmer bed: dark rumbling noise
    simmer = noise_band(n, lp=0.008, rng=rng)
    for i in range(n):
        buf[i] += simmer[i] * simmer_gain

    # crackle bloom under a bell envelope
    flame = crackle(int(1.1 * SR), rng)
    start = int(0.05 * SR)
    for i, s in enumerate(flame):
        j = start + i
        if j >= n:
            break
        tt = j / SR
        env = math.exp(-((tt - bell_center) ** 2) / (2 * bell_sigma * bell_sigma))
        buf[j] += s * crackle_gain * env

    # master envelope: quick fade in, gentle tail
    for i in range(n):
        tt = i / SR
        buf[i] *= min(1.0, tt / 0.12) * min(1.0, (dur - tt) / 0.6)

    # soft saturation lifts the RMS toward the base game select clips
    # (ironclad -16.4 dB, regent -15.7 dB), which are heavily compressed
    if drive > 0:
        peak = max(abs(s) for s in buf) or 1.0
        buf = [math.tanh(drive * s / peak) for s in buf]

    peak = max(abs(s) for s in buf)
    norm = 0.95 / peak if peak > 0 else 1.0
    frames = b"".join(
        struct.pack("<h", int(max(-1, min(1, s * norm)) * 32767)) for s in buf
    )
    os.makedirs(os.path.dirname(name), exist_ok=True)
    with wave.open(name, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(frames)
    print("wrote", os.path.normpath(name))


if __name__ == "__main__":
    render(OUT)
