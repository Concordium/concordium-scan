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
				'background-primary-elevated': 'var(--color-background-elevated)',
				'background-primary-elevated-nontrans':
					'var(--color-background-elevated-nontrans)',
				'button-primary': withHslOpacity('--color-button-bg-primary'),
				'button-primary-hover': withHslOpacity(
					'--color-button-bg-primary-hover'
				),
				'button-primary-disabled': 'var(--color-button-bg-primary-disabled)',
				'button-text-primary-disabled':
					'var(--color-button-text-primary-disabled)',
				'input-primary': withHslOpacity('--color-input-bg-primary', '25%'),
				'background-interactive': withHslOpacity('--color-interactive'),
			},
		},
		textColor: {
			theme: {
				white: 'white',
				body: withHslOpacity('--color-text-regular'),
				faded: 'var(--color-text-faded)',
				interactive: withHslOpacity('--color-interactive'),
				interactiveHover: withHslOpacity('--color-interactive-hover'),
				info: withHslOpacity('--color-info'),
				error: withHslOpacity('--color-error'),
			},
		},
		borderColor: {
			theme: {
				primary: withHslOpacity('--color-interactive'),
				selected: withHslOpacity('--color-selected'),
				white: withHslOpacity('--color-white'),
			},
		},
		extend: {},
	},
	variants: {
		extend: {},
	},
	plugins: [],
}
