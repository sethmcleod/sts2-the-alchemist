shader_type canvas_item;

// Contour swirl for the character-select background, alluding to a stirred cauldron.
// This runs on the gradient TextureRect itself: domain-warped noise is cut into flat
// bands, and each band multiplies the gradient toward dark, so the swirl deepens the
// hue underneath (deep purple over purple, deep green over green). Radial masks on
// the two pulsing lights thin the pattern where and when a light glows brighter.
// The twist is static. The motion comes from two slow sine drifts in the
// noise domain, so regions shift back and forth like the clouds above the Spire on
// the title screen. Nothing rotates.

uniform float strength : hint_range(0.0, 1.0) = 0.22;
uniform vec2 centre = vec2(0.6, 0.5);
uniform float scale = 2.5;
uniform float bands = 4.5;
uniform float swirl = 2.5;
uniform float speed = 0.05;
uniform float drift = 0.3;
// Band strength multiplier at the swirl centre, ramping to full toward the edges
uniform float centre_mask = 0.3;
// The masks are radial falloffs centred on the two pulsing lights, breathing with
// the same periods. CharSelectBgPatches pins each light's particle phase to global
// engine time, so a light's brightness peak and the mask's transparency peak line
// up: when a light pulses brighter, the swirl thins out around it. Positions are the
// light node positions in UV (divide by 1920x1080). light_clear is the maximum
// transparency at a pulse peak; light_floor is how much of it remains at the trough
uniform vec2 light_gold = vec2(0.781, 0.204);
uniform vec2 light_green = vec2(0.224, 0.75);
uniform float gold_period = 3.0;
uniform float green_period = 4.0;
uniform float light_radius = 0.45;
uniform float light_clear : hint_range(0.0, 1.0) = 0.8;
uniform float light_floor : hint_range(0.0, 1.0) = 0.2;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
			mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}

float fbm(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	for (int i = 0; i < 4; i++) {
		v += a * vnoise(p);
		p *= 2.03;
		a *= 0.5;
	}
	return v;
}

void fragment() {
	vec2 c = UV - centre;
	c.x *= 1.78;
	float r = length(c);
	float ang = atan(c.y, c.x) + swirl * exp(-1.3 * r);
	vec2 sw = vec2(cos(ang), sin(ang)) * r;
	vec2 osc = drift * vec2(sin(TIME * speed), sin(TIME * speed * 0.73 + 1.7));
	float n = fbm(sw * scale + osc + fbm(sw * scale - osc));
	float f = fract(n * bands);
	float band = smoothstep(0.35, 0.45, f) * (1.0 - smoothstep(0.85, 0.95, f));
	band *= mix(centre_mask, 1.0, smoothstep(0.15, 0.55, r));

	// Radial masks on the two lights, breathing with each light's pulse
	vec2 p = vec2(UV.x * 1.78, UV.y);
	float pulse_gold = sin(3.14159 * fract(TIME / gold_period));
	float pulse_green = sin(3.14159 * fract(TIME / green_period));
	float near_gold = 1.0 - smoothstep(0.0, light_radius, distance(p, vec2(light_gold.x * 1.78, light_gold.y)));
	float near_green = 1.0 - smoothstep(0.0, light_radius, distance(p, vec2(light_green.x * 1.78, light_green.y)));
	float clearing = max(near_gold * mix(light_floor, 1.0, pulse_gold),
			near_green * mix(light_floor, 1.0, pulse_green));
	band *= 1.0 - light_clear * clearing;

	// Multiply with a gray made from the band, so bands deepen the color underneath
	vec3 base = texture(TEXTURE, UV).rgb;
	COLOR = vec4(base * (1.0 - strength * band), 1.0);
}
