shader_type canvas_item;

// Only the robe purple moves. Its hues sit between 250 and 290 degrees, clear of the green skin
// (110), the gold eyes and staff orb (40) and the blue-grey trim (220)
uniform float band_center = 0.75;
uniform float band_half_width = 0.058;
uniform float band_softness = 0.02;
uniform float min_saturation = 0.18;

// The deep folds of the robe run on past the band toward red. The pants and the potions share
// those hues but are more saturated, thus on this side only dull pixels move. In turns, with red
// counted as 1.0 so the range does not wrap
uniform float red_side_start = 0.786;
uniform float red_side_end = 1.014;
uniform float red_side_softness = 0.02;
uniform float red_side_max_saturation = 0.33;

// Hue turns per second
uniform float cycle_speed = 0.1;

// COLOR in vertex() is the slot tint times modulate. The mask reads the texture alone, thus a tint
// such as the potion flash never moves it
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

void fragment() {
	vec4 texel = texture(TEXTURE, UV);
	vec3 hsv = rgb_to_hsv(texel.rgb);

	float distance_to_band = abs(fract(hsv.x - band_center + 0.5) - 0.5);
	float in_band = 1.0 - smoothstep(band_half_width, band_half_width + band_softness, distance_to_band);

	float unwrapped = hsv.x < 0.5 ? hsv.x + 1.0 : hsv.x;
	float red_side = smoothstep(red_side_start, red_side_start + red_side_softness, unwrapped)
		* (1.0 - smoothstep(red_side_end, red_side_end + red_side_softness, unwrapped))
		* (1.0 - smoothstep(red_side_max_saturation - 0.03, red_side_max_saturation + 0.03, hsv.y));

	float colorful = smoothstep(min_saturation * 0.5, min_saturation, hsv.y);

	vec3 cycled = hsv_to_rgb(vec3(fract(hsv.x + TIME * cycle_speed), hsv.y, hsv.z));
	texel.rgb = mix(texel.rgb, cycled, max(in_band, red_side) * colorful);
	COLOR = texel * tint;
}
