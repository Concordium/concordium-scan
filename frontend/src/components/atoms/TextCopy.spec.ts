import TextCopy from './TextCopy.vue'
import { setupComponent, screen, fireEvent } from '~/utils/testing'

const defaultProps = {
	text: 'Hello, World!',
	label: 'Click to copy super secret text',
}

const { render } = setupComponent(TextCopy, { defaultProps })

describe('TextCopy', () => {
	it('will have an accessible label', () => {
		render({})

		expect(screen.getByLabelText(defaultProps.label)).toBeInTheDocument()
	})

	it('will copy the text when clicked', () => {
		const writeTextSpy = jest.fn().mockImplementation(() => Promise.resolve())

		Object.assign(navigator, {
			clipboard: {
				writeText: writeTextSpy,
			},
		})

		render({})

		fireEvent.click(screen.getByLabelText(defaultProps.label))

		expect(writeTextSpy).toHaveBeenCalledWith(defaultProps.text)
	})
})
