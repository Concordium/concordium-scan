import {
	render as tlRender,
	screen,
	fireEvent,
	waitFor,
	within,
} from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'

/**
 * Set up Component for testing
 * @param Component - Base component to be tested
 * @param Defaults - Object containing defaultProps, defaultSlots and defaultAttrs
 * @returns render function to be called in each test with options to overwrite Component props, slots and attrs
 * @example
 * const { render } = setupComponent(Button, { defaultProps, defaultSlots, defaultAttrs })
 **/
const setupComponent = (
	Component: unknown,
	{
		defaultProps,
		defaultSlots,
		defaultAttrs,
	}: {
		defaultProps?: RenderOptions['props']
		defaultSlots?: RenderOptions['slots']
		defaultAttrs?: RenderOptions['attrs']
	}
) => {
	/**
	 * Render component with options to overwrite the initial config from setupComponent
	 * @param Options - Object containing props, slots and attrs
	 * @returns render function to be called in each test with options to overwrite Component props, slots and attributes
	 * @example
	 * render({ props, slots, attrs })
	 **/
	const render = ({
		props,
		slots,
		attrs,
	}: {
		props?: RenderOptions['props']
		slots?: RenderOptions['slots']
		attrs?: RenderOptions['attrs']
	}) => {
		tlRender(Component, {
			props: { ...defaultProps, ...props },
			slots: { ...defaultSlots, ...slots },
			attrs: { ...defaultAttrs, ...attrs },
		})
	}

	return { render }
}

export { setupComponent, screen, fireEvent, waitFor, within }
