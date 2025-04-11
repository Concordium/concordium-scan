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

/**
 * Utility to mock and spy on window.location
 * NB! Always call the cleanup function at the end of each test, to avoid side effects
 * @param overrides - Optional location properties to override
 * @returns render function to be called in each test with options to overwrite Component props, slots and attributes
 * @example
 * const { locationAssignSpy, locationCleanup } = mockLocation({ host: 'testnet.ccdscan.io' })
 **/
const mockLocation = (overrides?: Partial<Location>) => {
	const oldWindowLocation = window.location
	const locationAssignSpy = jest.fn()

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	delete (window as any).location

	Object.defineProperty(window, 'location', {
		writable: true,
		value: {
			...oldWindowLocation,
			protocol: 'https:',
			host: 'ccdscan.io',
			assign: locationAssignSpy,
			...overrides,
		},
	})

	const locationCleanup = () => {
		Object.defineProperty(window, 'location', {
			writable: true,
			value: oldWindowLocation,
		})
	}

	return {
		locationAssignSpy,
		locationCleanup,
	}
}

export { setupComponent, screen, fireEvent, waitFor, within, mockLocation }
