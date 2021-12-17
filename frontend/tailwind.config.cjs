/**
 * A utility function to attach tailwind opacity to hsl color variables.
 * This is required so utility classes for opacity are applied correctly to custom color variables.
 *
 * @example
 * --color-red: 255, 0, 0;
 * withHslOpacity("--color-red")
 *
 * @param { string } variableName css variable (as hsl) to attach opacity to.
 * @param { bool } important adds !important to the value in case it's important.
 * @returns { string } rgba value containing opacity, if available.
 */
const withHslOpacity = (variableName, important) => {
	return vars => {
		const { opacityValue } = vars
		let result
		result = `hsla(var(${variableName}))`
		if (opacityValue !== undefined) {
			result = `hsla(var(${variableName}), ${opacityValue})`
		}
		if (important) {
			result += '!important'
		}
		return result
	}
}

module.exports = {
	purge: ['./*.{vue,js,ts,css}', './**/*.{vue,js,ts,css}'],
	darkMode: false, // or 'media' or 'class'
	theme: {
		fontFamily: {
			mono: 'var(--font-family-mono)',
			sans: 'var(--font-family-primary)',
		},
		backgroundColor: {
			theme: {
				'common-white': withHslOpacity('--color-white'),
				'background-primary': withHslOpacity('--color-background-primary'),
				'button-primary': withHslOpacity('--color-button-bg-primary'),
				'button-primary-hover': withHslOpacity(
					'--color-button-bg-primary-hover'
				),
				'input-primary': withHslOpacity('--color-input-bg-primary', '25%'),
			},
		},
		extend: {},
	},
	variants: {
		extend: {},
	},
	plugins: [],
}
