shader_type canvas_item;

uniform bool robe = false;
uniform bool skin = false;
uniform bool bag = false;
uniform bool eyes = false;
uniform bool orb = false;
uniform bool orb_light = false;

uniform float cycle_speed = 0.1;

uniform float robe_center = 0.75;
uniform float robe_half_width = 0.058;
uniform float robe_softness = 0.02;
uniform float min_saturation = 0.18;

uniform float red_side_start = 0.786;
uniform float red_side_end = 1.014;
uniform float red_side_softness = 0.02;
uniform float red_side_max_saturation = 0.33;

uniform float skin_center = 0.301;
uniform float skin_half_width = 0.0375;
uniform float skin_min_saturation = 0.3;

uniform float bag_center = 0.046;
uniform float bag_half_width = 0.032;
uniform float bag_min_saturation = 0.4;

uniform float gold_center = 0.097;
uniform float gold_half_width = 0.056;
uniform float glow_lift = 0.3;
uniform float glow_chroma_scale = 0.85;

uniform float light_center = 0.153;
uniform float light_half_width = 0.069;
uniform float light_min_saturation = 0.55;
uniform float light_min_value = 0.45;

uniform vec4 left_eye = vec4(0.0);
uniform vec4 right_eye = vec4(0.0);
uniform float eye_light_min_value = 0.86;

varying vec4 tint;

void vertex() {
	tint = COLOR;
}

vec3 rgb_to_hsv(vec3 c) {
	vec4 k = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(c.bg, k.wz), vec4(c.gb, k.xy), step(c.b, c.g));
	vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
	float d = q.x - min(q.w, q.y);
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + 1.0e-10)), d / (q.x + 1.0e-10), q.x);
}

vec3 hsv_to_rgb(vec3 c) {
	vec4 k = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + k.xyz) * 6.0 - k.www);
	return c.z * mix(k.xxx, clamp(p - k.xxx, 0.0, 1.0), c.y);
}

float hue_band(float hue, float center, float half_width, float softness) {
	float distance_to_center = abs(fract(hue - center + 0.5) - 0.5);
	return 1.0 - smoothstep(half_width, half_width + softness, distance_to_center);
}

float robe_mask(vec3 hsv) {
	float in_band = hue_band(hsv.x, robe_center, robe_half_width, robe_softness);
	float unwrapped = hsv.x < 0.5 ? hsv.x + 1.0 : hsv.x;
	float red_side = smoothstep(red_side_start, red_side_start + red_side_softness, unwrapped)
		* (1.0 - smoothstep(red_side_end, red_side_end + red_side_softness, unwrapped))
		* (1.0 - smoothstep(red_side_max_saturation - 0.03, red_side_max_saturation + 0.03, hsv.y));
	return max(in_band, red_side);
}

float gold_mask(vec3 hsv) {
	return hue_band(hsv.x, gold_center, gold_half_width, 0.03)
		* smoothstep(0.2, 0.3, hsv.y) * smoothstep(0.3, 0.45, hsv.z);
}

float light_mask(vec3 hsv) {
	return hue_band(hsv.x, light_center, light_half_width, 0.02)
		* smoothstep(light_min_saturation - 0.06, light_min_saturation + 0.06, hsv.y)
		* smoothstep(light_min_value - 0.06, light_min_value + 0.06, hsv.z);
}

float ellipse(vec2 uv, vec4 shape) {
	if (shape.z <= 0.0) {
		return 0.0;
	}
	return 1.0 - smoothstep(1.0, 1.04, length((uv - shape.xy) / shape.zw));
}

vec3 srgb_to_linear(vec3 c) {
	return mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(0.04045, c));
}

vec3 linear_to_srgb(vec3 c) {
	return mix(c * 12.92, 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055, step(0.0031308, c));
}

vec3 linear_to_oklab(vec3 c) {
	float l = pow(max(0.4122214708 * c.r + 0.5363325363 * c.g + 0.0514459929 * c.b, 0.0), 1.0 / 3.0);
	float m = pow(max(0.2119034982 * c.r + 0.6806995451 * c.g + 0.1073969566 * c.b, 0.0), 1.0 / 3.0);
	float s = pow(max(0.0883024619 * c.r + 0.2817188376 * c.g + 0.6299787005 * c.b, 0.0), 1.0 / 3.0);
	return vec3(
		0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
		1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
		0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
}

vec3 oklab_to_linear(vec3 c) {
	float l = c.x + 0.3963377774 * c.y + 0.2158037573 * c.z;
	float m = c.x - 0.1055613458 * c.y - 0.0638541728 * c.z;
	float s = c.x - 0.0894841775 * c.y - 1.2914855480 * c.z;
	l = l * l * l;
	m = m * m * m;
	s = s * s * s;
	return vec3(
		4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
		-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
		-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
}

vec3 light_rainbow(vec3 rgb, float turns) {
	vec3 lab = linear_to_oklab(srgb_to_linear(rgb));
	float angle = turns * 6.2831853;
	float cosine = cos(angle);
	float sine = sin(angle);
	vec2 ab = vec2(lab.y * cosine - lab.z * sine, lab.y * sine + lab.z * cosine) * glow_chroma_scale;
	vec3 turned = vec3(mix(lab.x, 1.0, glow_lift), ab);
	return clamp(linear_to_srgb(max(oklab_to_linear(turned), vec3(0.0))), 0.0, 1.0);
}

void fragment() {
	vec4 texel = texture(TEXTURE, UV);
	vec3 hsv = rgb_to_hsv(texel.rgb);
	float turns = TIME * cycle_speed;

	float body = 0.0;
	if (robe) {
		body = max(body, robe_mask(hsv));
	}
	if (skin) {
		body = max(body, hue_band(hsv.x, skin_center, skin_half_width, 0.02)
			* smoothstep(skin_min_saturation - 0.04, skin_min_saturation + 0.04, hsv.y));
	}
	if (bag) {
		body = max(body, hue_band(hsv.x, bag_center, bag_half_width, 0.02)
			* smoothstep(bag_min_saturation - 0.04, bag_min_saturation + 0.04, hsv.y));
	}
	body *= smoothstep(min_saturation * 0.5, min_saturation, hsv.y);

	vec3 cycled = hsv_to_rgb(vec3(fract(hsv.x + turns), hsv.y, hsv.z));
	texel.rgb = mix(texel.rgb, cycled, body);

	vec4 color = texel * tint;
	if (eyes || orb || orb_light) {
		float gold = max(gold_mask(hsv), gold_mask(rgb_to_hsv(tint.rgb)));
		float in_eye = max(ellipse(UV, left_eye), ellipse(UV, right_eye));
		float lit = light_mask(hsv)
			* mix(1.0, smoothstep(eye_light_min_value - 0.02, eye_light_min_value + 0.02, hsv.z), in_eye);

		float glow = orb ? gold : 0.0;
		if (eyes) {
			bool mapped = left_eye.z > 0.0 || right_eye.z > 0.0;
			glow = max(glow, mapped ? gold * in_eye * (1.0 - lit) : gold);
		}
		if (orb_light) {
			glow = max(glow, lit);
		}
		color.rgb = mix(color.rgb, light_rainbow(color.rgb, turns), glow);
	}
	COLOR = color;
}
