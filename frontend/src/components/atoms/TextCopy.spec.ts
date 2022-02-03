import { render, screen } from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'
import { fireEvent } from '@testing-library/dom'
import TextCopy from './TextCopy.vue'

const defaultProps = {
	text: 'Hello, World!',
	label: 'Click to copy super secret text',
}

const renderComponent = (props?: RenderOptions['props']) =>
	render(TextCopy, { props: { ...defaultProps, ...props } })

describe('TextCopy', () => {
	it('will have an accessible label', () => {
		renderComponent()

		expect(screen.getByLabelText(defaultProps.label)).toBeInTheDocument()
	})

	it('will copy the text when clicked', () => {
		const writeTextSpy = jest.fn().mockImplementation(() => Promise.resolve())

		Object.assign(navigator, {
			clipboard: {
				writeText: writeTextSpy,
			},
		})

		renderComponent()

		fireEvent.click(screen.getByLabelText(defaultProps.label))

		expect(writeTextSpy).toHaveBeenCalledWith(defaultProps.text)
	})
})
